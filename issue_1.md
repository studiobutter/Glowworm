# GitHub Issue [#21](https://github.com/studiobutter/Glowworm/issues/21)
- [ ] Add ZZZ - XBOX for PC version.

in v3.1 special program, HoYo have officially announced that ZZZ is coming to XBOX for PC version and has optimized for Windows Handheld.

Upcoming update will add support for that version. To do so, the app will need to detect where the game is installed. 

We can detect where to locate where the game is installed by reading the `.GamingRoot` in every Drives which also contain the install location of the drive.

Example: 
- Install Location: E:/XboxGames
- `.GamingRoot` file: 
  -  In Hex: 
```
52 47 42 58 01 00 00 00 58 00 62 00 6f 00 78 00 47 00 61 00 6d 00 65 00 73 00 00 00
```
  - Decoded: 
```
R G B X . . . . X . b . o . x . G . a . m . e . s . . .
```
  - Encoded in UTF-16 LE: 
```
䝒塂 XboxGames 
```

- Install Location: E:/Games/XboxGames
- `.GamingRoot` file:
  -  In Hex: 
```
52 47 42 58 01 00 00 00 47 00 61 00 6d 00 65 00 73 00 5c 00 58 00 62 00 6f 00 78 00 47 00 61 00 6d 00 65 00 73 00 00 00
```
  - Decoded: 
```
R G B X . . . . G . a . m . e . s . \ . X . b . o . x . G . a . m . e . s . . .
```
  - Encoded in UTF-16 LE: 
```
䝒塂 Games\XboxGames 
```

From there we can find: `[Install Directory]\Zenless Zone Zero\Content\`

Where screenshots `[Install Directory]\Zenless Zone Zero\Content\ScreenShot` is.

Where Gacha URL Cached file is: `[Install Directory]\Zenless Zone Zero\Content\ZenlessZoneZero_Data\webCaches\[WebView Version]\Cache\Cache_Data`

More information will be announced soon!

- [ ] [fix] Add indicator to know which game tab the user is on.
- [ ] [fix] Locating `data_2` file