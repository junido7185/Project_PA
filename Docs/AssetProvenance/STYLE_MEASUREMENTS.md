# ART-000 Style Measurements

같은 원본도 FBX/OBJ/BLEND의 triangulation과 split normals 때문에 vertex/poly 수가 다를 수 있다. 아래 polygon은 Blender mesh polygon, triangle은 OBJ fan-triangulation, surface density는 source local mesh area 기준이다. 단위/축이 다른 팩을 source density만으로 순위 매기지 않는다.

| ID | Blender polygons | OBJ triangles | tri / source area | smooth polygons % | live bevel modifiers | colors sampled | mean saturation | roughness / metallic |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| `PA_REF_MINIMARKET_CASH_REGISTER` | 203 | 406 | 67.43 | 62.1 | 0 | 19 | 0.255 | 1.00/0.00 |
| `PA_REF_MINIMARKET_DISPLAY_BREAD` | 362 | 460 | 77.58 | 63.3 | 0 | 32 | 0.371 | 1.00/0.00 |
| `PA_REF_MINIMARKET_DISPLAY_FRUIT` | 650 | 1300 | 387.91 | 85.7 | 0 | 16 | 0.644 | 1.00/0.00 |
| `PA_REF_MINIMARKET_SHELF_END` | 362 | 450 | 65.74 | 48.6 | 0 | 22 | 0.290 | 1.00/0.00 |
| `PA_REF_MINIMARKET_SHOPPING_BASKET` | 288 | 576 | 423.11 | 52.4 | 0 | 9 | 0.253 | 1.00/0.00 |
| `PA_REF_FOODKIT_BAG` | 46 | 92 | 58.12 | 95.7 | 0 | 5 | 0.444 | 1.00/0.00 |
| `PA_REF_FOODKIT_BARREL` | 308 | 616 | 122.20 | 19.8 | 0 | 22 | 0.374 | 1.00/0.00 |
| `PA_REF_FOODKIT_CARROT` | 148 | 296 | 521.55 | 98.6 | 0 | 8 | 0.643 | 1.00/0.00 |
| `PA_REF_FOODKIT_CUTTING_BOARD` | 120 | 240 | 108.20 | 43.3 | 0 | 4 | 0.522 | 1.00/0.00 |
| `PA_REF_FOODKIT_FISH` | 233 | 466 | 725.05 | 93.6 | 0 | 20 | 0.200 | 1.00/0.00 |
| `PA_REF_FOODKIT_LOAF` | 116 | 232 | 109.38 | 25.9 | 0 | 13 | 0.514 | 1.00/0.00 |
| `PA_REF_FOODKIT_MUSHROOM` | 80 | 160 | 1415.27 | 60.0 | 0 | 6 | 0.558 | 1.00/0.00 |
| `PA_REF_FOODKIT_PLATE` | 140 | 280 | 105.25 | 84.3 | 0 | 5 | 0.057 | 1.00/0.00 |
| `PA_REF_FOODKIT_POT` | 228 | 456 | 118.23 | 59.6 | 0 | 6 | 0.171 | 1.00/0.00 |
| `PA_REF_MINIARCADE_ARCADE_MACHINE` | 392 | 784 | 213.45 | 76.5 | 0 | 25 | 0.318 | 1.00/0.00 |
| `PA_REF_MINIARCADE_PRIZES` | 464 | 928 | 133.77 | 51.1 | 0 | 25 | 0.327 | 1.00/0.00 |
| `PA_REF_CUTEFISH_DOCK_LONG_NOROPE` | 702 | 1484 | 2.58 | 0.0 | 0 | 2 | 0.536 | 0.50/0.00 |
| `PA_REF_CUTEFISH_FISHINGROD_LVL1` | 152 | 308 | 117.62 | 0.0 | 0 | 1 | 0.607 | 0.50/0.00 |
| `PA_REF_CUTEFISH_KOI` | 762 | 1494 | 42.42 | 31.5 | 0 | 5 | 0.272 | 0.50/0.00 |
| `PA_REF_CUTEFISH_TUNA` | 732 | 1446 | 41.07 | 0.0 | 0 | 5 | 0.295 | 0.50/0.00 |
| `PA_REF_ULTIMATENATURE_BIRCHTREE_1` | 880 | 1704 | 66.03 | 0.0 | 0 | 4 | 0.322 | 0.50/0.00 |
| `PA_REF_ULTIMATENATURE_BUSH_1` | 182 | 364 | 46.89 | 0.0 | 0 | 1 | 0.607 | 0.50/0.00 |
| `PA_REF_ULTIMATENATURE_COMMONTREE_1` | 1444 | 2888 | 213.25 | 0.0 | 0 | 2 | 0.609 | 0.50/0.00 |
| `PA_REF_ULTIMATENATURE_FLOWERS` | 220 | 408 | 932.46 | 0.0 | 0 | 3 | 0.785 | 0.50/0.00 |
| `PA_REF_ULTIMATENATURE_GRASS_SHORT` | 108 | 192 | 604.96 | 0.0 | 0 | 1 | 0.607 | 0.50/0.00 |
| `PA_REF_ULTIMATENATURE_ROCK_1` | 36 | 70 | 54.09 | 0.0 | 0 | 1 | 0.195 | 0.50/0.00 |
| `PA_REF_ULTIMATENATURE_WHEAT` | 212 | 408 | 628.10 | 0.0 | 0 | 1 | 0.749 | 0.50/0.00 |
| `PA_REF_ULTIMATENATURE_WOODLOG` | 242 | 464 | 59.90 | 0.0 | 0 | 3 | 0.451 | 0.50/0.00 |
| `PA_REF_CUBEWORLDKIT_CART` | 14 | 28 | 1.33 | 0.0 | 0 | 1 | 0.000 | 0.50/0.00 |
| `PA_REF_CUBEWORLDKIT_CHEST_CLOSED` | 1420 | 2914 | 57.87 | 45.1 | 0 | 3 | 0.349 | 0.50/0.00 |
| `PA_REF_CUBEWORLDKIT_CHEST_OPEN` | 1420 | 2914 | 57.87 | 45.1 | 0 | 3 | 0.349 | 0.50/0.00 |
| `PA_REF_CUBEWORLDKIT_AXE_STONE` | 123 | 236 | 97.83 | 100.0 | 0 | 2 | 0.262 | 0.50/0.00 |
| `PA_REF_CUBEWORLDKIT_PICKAXE_STONE` | 172 | 328 | 135.42 | 100.0 | 0 | 2 | 0.262 | 0.50/0.00 |

## 팩별 선정 표본 평균

| Pack | n | mean Blender polygons | mean OBJ triangles | mean tri/source area |
|---|---:|---:|---:|---:|
| MiniMarket | 5 | 373.0 | 638.4 | 204.35 |
| FoodKit | 9 | 157.7 | 315.3 | 364.81 |
| MiniArcade | 2 | 428.0 | 856.0 | 173.61 |
| CuteFish | 4 | 587.0 | 1183.0 | 50.92 |
| UltimateNature | 8 | 415.5 | 812.2 | 325.71 |
| CubeWorldKit | 5 | 629.8 | 1284.0 | 70.06 |
