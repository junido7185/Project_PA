using UnityEngine;

public class Farmland : MonoBehaviour
{
    public bool isOccupied = false; // 이미 심었나?
    public GameObject currentCrop;  // 심겨진 작물
    public Crop CurrentCrop => currentCrop != null ? currentCrop.GetComponent<Crop>() : null;

    public bool Plant(GameObject cropPrefab)
    {
        if (isOccupied && currentCrop == null)
            isOccupied = false;

        if (isOccupied)
        {
            Debug.Log("🚫 이미 작물이 심어져 있습니다!");
            return false;
        }

        if (cropPrefab == null || cropPrefab.GetComponent<Crop>() == null)
        {
            Debug.LogWarning("🌱 유효한 Crop 프리팹이 없어 심을 수 없습니다.");
            return false;
        }

        // 씨앗(작물) 생성 (위치는 밭 정중앙)
        currentCrop = Instantiate(cropPrefab, transform.position, transform.rotation);
        
        // 밭을 작물의 부모로 설정 (밭 옮기면 작물도 따라가게)
        currentCrop.transform.SetParent(transform);
        currentCrop.transform.localPosition = Vector3.zero;
        currentCrop.transform.localRotation = Quaternion.identity;
        
        isOccupied = true;
        Debug.Log("🌱 씨앗을 심었습니다!");
        return true;
    }

    // (나중을 위해) 작물 수확 후 초기화 함수
    public void ClearLand()
    {
        isOccupied = false;
        currentCrop = null;
    }

    public void ClearLandAndDestroyCrop()
    {
        GameObject crop = currentCrop;
        ClearLand();
        if (crop == null) return;

        crop.SetActive(false);
        if (Application.isPlaying) Destroy(crop);
        else DestroyImmediate(crop);
    }
}
