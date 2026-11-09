using System;
using UnityEngine;

// Interaction proxy delegates rewards to existing gathering/farming/mining and producer inventories.
public sealed class StarterWorksiteInteraction : MonoBehaviour, IInteractable
{
    public WorksiteBinding Binding { get; private set; }
    public string Owner => Binding.CompanionId;
    public ProducerNpcController Producer => Binding != null ? Binding.Producer : null;
    public bool PlayerActivityCompleted { get; private set; }
    public bool SeedIssued { get; private set; }
    public FarmPlotInteraction Farm { get; private set; }
    public string Status => Producer.StarterClaimed ? "지원 물량 수령 완료" : Producer.StarterBatchReady ? "첫 생산 " + Item.itemName + " ×" + Producer.StarterStockCount + " · SPACE 수령" :
        Producer.StarterWorking ? "첫 " + Item.itemName + " 생산 중" : "SPACE · " + (Profile.activityKind==WorksiteActivityKind.Farming ? "파종 / 수확 체험" : Profile.activityKind==WorksiteActivityKind.Mining ? "광맥 채굴 체험" : "공용 도끼로 원목 채집");
    public Item Item => Producer.productionData.producedItem;
    StarterWorksiteProfile Profile => Binding.Profile;
    GameObject _npc, _activity;
    MiningSpot _mining;
    DaytimeStockPrepPoint _stock;
    Transform _tool, _batchVisual;
    PrototypeWorldLabel _label;
    string _lastStatus;

    public void Configure(WorksiteBinding binding)
    {
        if (Binding != null || binding == null || !binding.Assigned)
            throw new InvalidOperationException("Worksite interaction requires one assigned binding.");
        Binding=binding; _npc=binding.gameObject;
        var labelRoot=new GameObject("ProductionLabel");labelRoot.transform.SetParent(transform,false);labelRoot.transform.localPosition=new Vector3(0,1.8f,0);
        _label=labelRoot.AddComponent<PrototypeWorldLabel>();
        _activity=new GameObject("ExistingPlayerActivity");_activity.transform.SetParent(transform,false);_activity.transform.localPosition=Vector3.up*.13f;
        var activityLabel=new GameObject("Label");activityLabel.transform.SetParent(_activity.transform,false);activityLabel.AddComponent<PrototypeWorldLabel>();
        if(Profile.activityKind==WorksiteActivityKind.Farming)
        {
            var col=_activity.AddComponent<BoxCollider>();col.isTrigger=true;col.enabled=false;
            _activity.AddComponent<Farmland>();
            Farm=_activity.AddComponent<FarmPlotInteraction>(); Farm.Configure("p4-farm-"+Owner,"정착 실습 밭");Farm.growthSecondsPerStage=3;
            activityLabel.SetActive(false);
        }
        else if(Profile.activityKind==WorksiteActivityKind.Mining)
        {
            _stock=_activity.AddComponent<DaytimeStockPrepPoint>();_stock.Configure("p4-mine-"+Owner,"Items/Item_Ore",2,"정착 실습 광맥");
            _mining=_activity.AddComponent<MiningSpot>();_mining.Configure(_stock);activityLabel.SetActive(false);
        }
        var toolPrefab=Profile.toolPrefab;
        if(toolPrefab!=null)
        {
            _tool=Instantiate(toolPrefab,_npc.transform).transform;_tool.name="P4WorkTool_"+Owner;
            _tool.localPosition=new Vector3(.38f,.8f,.32f);_tool.gameObject.SetActive(false);
        }
        var batch=new GameObject("ActualFirstBatch");batch.transform.SetParent(transform,false);batch.transform.localPosition=new Vector3(.45f,.2f,-.15f);_batchVisual=batch.transform;
        if(Item.model!=null)
        {
            var model=Instantiate(Item.model,_batchVisual);model.transform.localScale=Vector3.one*.5f;
            foreach(var c in model.GetComponentsInChildren<Collider>()) c.enabled=false;
        }
        _batchVisual.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if(_tool!=null) Destroy(_tool.gameObject);
    }
    void Update()
    {
        if(Producer==null) return;
        if(!PlayerActivityCompleted && _stock!=null && DayNightShopLoopController.Instance.IsDailyActivityCompleted(_stock.activityId)) CompleteActivity();
        bool working=Producer.GetFsmState()=="Working" && Producer.StarterWorking;
        if(working)
        {
            var facing=transform.position-_npc.transform.position;facing.y=0;
            if(facing.sqrMagnitude>.01f)_npc.transform.rotation=Quaternion.LookRotation(facing);
        }
        if(_tool!=null)
        {
            _tool.gameObject.SetActive(working);
            float swing=Mathf.Sin(Time.time*(Profile.activityKind==WorksiteActivityKind.Farming?3:6));
            _tool.localRotation=Profile.activityKind==WorksiteActivityKind.Farming?Quaternion.Euler(25+swing*18,0,-20):Quaternion.Euler(-30+swing*55,0,Profile.activityKind==WorksiteActivityKind.Mining?25:-25);
        }
        _batchVisual.gameObject.SetActive(Producer.StarterBatchReady);
        if(_lastStatus!=Status){_lastStatus=Status;_label.Set(Status,new Color(.17f,.23f,.23f),1.65f);}
    }

    public string GetInteractPrompt() => Status;
    public void Interact(GameObject interactor)
    {
        if(Producer.StarterBatchReady)
        {
            Producer.Procurement.TryClaimSettlementSupport(Inventory.instance);return;
        }
        // Player activity remains available after NPC work begins; free NPC support is only once.
        int before=Inventory.instance.CountItems(Item);
        if(Profile.activityKind==WorksiteActivityKind.Farming)
        {
            if(!SeedIssued)
            {
                var seed=Profile.starterSeed;
                if(!Inventory.instance.CanAddItems(seed,1) || !Inventory.instance.AddItem(seed,1)) return;
                SeedIssued=true;
            }
            Farm.Interact(interactor);
        }
        else if(Profile.activityKind==WorksiteActivityKind.Mining) _mining.Interact(interactor);
        else
        {
            if(!Inventory.instance.CanAddItems(Item,1)) return;
            var log=new GameObject("PlayerHarvestLog");log.transform.SetParent(_activity.transform,false);
            var gather=log.AddComponent<Gatherable>();gather.dropItem=Item;gather.requiredTool=Profile.gatheringTool;
            gather.Interact(interactor);
        }
        if(Inventory.instance.CountItems(Item)>before) CompleteActivity();
    }
    void CompleteActivity()
    {
        if(PlayerActivityCompleted)return;
        PlayerActivityCompleted=true;Producer.StartStarterWork();
        Debug.Log("[VS-P4] PLAYER_ACTIVITY "+Owner+" "+Item.itemName);
    }
    public void Restore(StarterWorksiteSaveData saved)
    {
        PlayerActivityCompleted=saved.playerActivityCompleted;SeedIssued=saved.seedIssued;
        if(Farm!=null) Farm.RestoreSaveState(saved.farm);
        Producer.RestoreStarterState(saved.producer);
    }
}
