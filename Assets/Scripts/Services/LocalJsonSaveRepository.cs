using System.IO;
using System.Threading.Tasks;
using UnityEngine;

// Application.persistentDataPath 하위에 key.json 으로 저장하는 로컬 구현체.
// 나중에 UGSCloudSaveRepository 가 추가되면 같은 인터페이스로 교체만 하면 된다.
public class LocalJsonSaveRepository : ISaveRepository
{
    private readonly string _rootPath;

    public LocalJsonSaveRepository()
    {
        _rootPath = Application.persistentDataPath;
    }

    public LocalJsonSaveRepository(string customRoot)
    {
        _rootPath = customRoot;
    }

    private string PathFor(string key) => Path.Combine(_rootPath, $"{key}.json");

    public Task<string> LoadAsync(string key)
    {
        string path = PathFor(key);
        if (!File.Exists(path)) return Task.FromResult<string>(null);
        // Unity의 메인 스레드 제약 때문에 MVP는 동기 I/O 를 Task로 감싸 반환한다.
        // Cloud Save 구현체는 진짜 async I/O를 사용하게 된다.
        string content = File.ReadAllText(path);
        return Task.FromResult(content);
    }

    public Task SaveAsync(string key, string json)
    {
        string path = PathFor(key);
        File.WriteAllText(path, json);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(File.Exists(PathFor(key)));
    }
}
