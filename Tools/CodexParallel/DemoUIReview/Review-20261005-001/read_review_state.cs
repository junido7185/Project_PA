// Read only. No input, scene control, SessionState writes or game authority mutations.
return new {
    playing = UnityEditor.EditorApplication.isPlaying,
    changing = UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode,
    compiling = UnityEditor.EditorApplication.isCompiling,
    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
    gatherReviewActive = UnityEditor.SessionState.GetBool("PA.P3Gather.Active", false),
    gatherReviewMode = UnityEditor.SessionState.GetString("PA.P3Gather.Mode", ""),
    gatherReviewOut = UnityEditor.SessionState.GetString("PA.P3Gather.Out", ""),
    businessReviewActive = UnityEditor.SessionState.GetBool("PA.DemoBusinessPlayReview.Active", false),
    isolatedSaveRoot = UnityEditor.SessionState.GetString(SaveManager.ValidationRootSessionKey, ""),
    storageOpen = StorageUI.instance != null && StorageUI.instance.IsOpen,
    craftingOpen = CraftingUI.instance != null && CraftingUI.instance.IsOpen,
    priceOpen = ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen
};
