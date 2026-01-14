using UnityEngine;

public class Chair : MonoBehaviour
{
    // 앉을 위치 (의자 방석 위)
    public Transform sitPoint; 
    
    // 누군가 앉아있는지 체크
    public bool isOccupied = false; 
}