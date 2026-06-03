# Screenshot Improvement

Improve upon the existing Screenshot function of the app.

Improve existing screenshot system detection with the following:
- Process/Window Detection: 
  - Genshin Impact:
    - Local Install:
      - Global (Official): GenshinImpact.exe
      - Global (Google Play): GenshinImpact.exe
      - Global (Epic Games): GenshinImpact.exe
      - China (Official): YuanShen.exe
      - China (BiliBili): YuanShen.exe
    - Cloud version: 
      - Global: Genshin Impact Cloud.exe (Executable has space)
      - China: Genshin Impact Cloud Game.exe (Executable has space)
    - Honkai: Star Rail:
      - Local Install:
        - Global (Official): StarRail.exe
        - Global (Epic Games): StarRail.exe
        - China (Official): StarRail.exe
        - China (BiliBili): StarRail.exe
    - Zenless Zone Zero
      - Local Install:
        - Global (Official): ZenlessZoneZero.exe
        - Global (Epic Games): ZenlessZoneZero.exe
        - China (Official): ZenlessZoneZero.exe
        - China (BiliBili): ZenlessZoneZero.exe
      - Cloud version:
        - Global: Zenless Zone Zero Cloud.exe (Executable has space)
        - China: Zenless Zone Zero Cloud.exe (Executable has space)

Above has been improved, need to fix the Folder path issue. 

No matter the region, 
- If ZZZ or ZZZ Cloud Screenshot taken, store in ScreenshotDir/ZZZ
- If Star Rail Screenshot taken, store in ScreenshotDir/Star Rail
- If Genshin or Genshin Cloud Screenshot taken, store in ScreenshotDir/Genshin