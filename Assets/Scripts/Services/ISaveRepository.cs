using System.Threading.Tasks;

// 저장소 백엔드 추상화.
//
// 설계 의도:
// - MVP에서는 LocalJsonSaveRepository 하나만 쓰지만, 나중에 UGSCloudSaveRepository를
//   추가하고 SaveManager가 참조하는 구현체만 바꾸면 Docs/04의 Cloud Save 시나리오로 그대로 이관된다.
// - async 시그니처는 로컬 파일 I/O 에는 과할 수 있지만, Cloud Save 는 반드시 비동기이므로
//   지금부터 async 규약으로 묶어 두면 교체 시 호출처를 건드릴 필요가 없다.
public interface ISaveRepository
{
    // key로 저장된 JSON 문자열을 읽는다. 없으면 null을 반환한다.
    Task<string> LoadAsync(string key);

    // key에 JSON 문자열을 저장한다.
    Task SaveAsync(string key, string json);

    // key의 데이터가 존재하는지 확인한다.
    Task<bool> ExistsAsync(string key);
}
