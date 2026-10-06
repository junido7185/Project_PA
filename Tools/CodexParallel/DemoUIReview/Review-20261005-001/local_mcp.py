"""Use the already running local MCP connection; no installation or routing changes."""
import argparse
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).resolve().parents[4]
TOOL = Path(__file__).resolve().parent
LOG = ROOT / 'Logs/CodexParallel/DemoUIReview/Review-20261005-001'


class LocalClient:
    def __init__(self):
        config = json.loads((ROOT / '.mcp.json').read_text(encoding='utf-8'))
        self.url = config['mcpServers']['unityMCP']['url']
        if self.url != 'http://127.0.0.1:8080/mcp':
            raise RuntimeError('Unexpected endpoint; no remote connection')
        self.headers = {'Content-Type': 'application/json', 'Accept': 'application/json, text/event-stream'}
        self.seq = 0
        result = self.rpc('initialize', {'protocolVersion': '2024-11-05', 'capabilities': {},
            'clientInfo': {'name': 'CodexParallelIsolatedReview', 'version': '1'}})
        if 'result' not in result:
            raise RuntimeError('Initialization failed')
        self.rpc('notifications/initialized', notification=True)

    def rpc(self, method, params=None, notification=False):
        self.seq += 1
        payload = {'jsonrpc': '2.0', 'method': method}
        if not notification:
            payload['id'] = self.seq
        if params is not None:
            payload['params'] = params
        request = urllib.request.Request(self.url, data=json.dumps(payload).encode(), headers=self.headers, method='POST')
        with urllib.request.urlopen(request, timeout=45) as response:
            if response.headers.get('Mcp-Session-Id'):
                self.headers['Mcp-Session-Id'] = response.headers['Mcp-Session-Id']
            body = response.read().decode('utf-8')
        if not body.strip():
            return {}
        if body.lstrip().startswith('{'):
            return json.loads(body)
        entries = [json.loads(line[5:].strip()) for line in body.splitlines() if line.startswith('data:')]
        return next((item for item in entries if item.get('id') == payload.get('id')), entries[-1] if entries else {})


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=['discover', 'execute'])
    parser.add_argument('record')
    parser.add_argument('--code')
    parser.add_argument('--compiler', choices=['roslyn', 'codedom'], default='roslyn')
    args = parser.parse_args()
    if not args.record.replace('-', '').replace('_', '').isalnum():
        parser.error('Record must be a plain filename')
    output = LOG / (args.record + '.json')
    if output.exists():
        raise RuntimeError('Do not overwrite an observation')
    client = LocalClient()
    if args.action == 'discover':
        response = client.rpc('tools/list', {})
        response = {'tools': [t for t in response.get('result', {}).get('tools', []) if t['name'] == 'execute_code']}
    else:
        source = (TOOL / (args.code or '')).resolve()
        source.relative_to(TOOL)
        if source.suffix != '.cs':
            parser.error('Code must be a reviewed C# snippet in this task folder')
        response = client.rpc('tools/call', {'name': 'execute_code', 'arguments': {
            'action': 'execute', 'code': source.read_text(encoding='utf-8'), 'compiler': args.compiler, 'safety_checks': True}})
    LOG.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(response, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(response, ensure_ascii=True))


if __name__ == '__main__':
    main()
