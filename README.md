Skipdoor Delivery
===

[![RimWorld](https://img.shields.io/badge/RimWorld-1.3-informational)](https://rimworldgame.com/) [![Steam Downloads](https://img.shields.io/steam/downloads/2854735284)](https://steamcommunity.com/sharedfiles/filedetails/?id=2854735284) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) [![Contributor Covenant](https://img.shields.io/badge/Contributor%20Covenant-2.1-4baaaa.svg)](CODE_OF_CONDUCT.md)

[Skipdoor Delivery](https://steamcommunity.com/sharedfiles/filedetails/?id=2854735284) is a [RimWorld](https://rimworldgame.com/) patch for the skipdoor psycast from the [Vanilla Psycasts Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2842502659) mod.

With this mod, skipdoors can teleport items to other skipdoors automatically, even if they are in different maps. You can use skipdoors to send your hard-earned loot back to your base, to move items around your colony, or even to help manage simultaneous colonies.

### How does it work?

After creating your skipdoors, you must place a stockpile zone under each skipdoor that needs to send or receive items. The configuration of these stockpile zones is used by the skipdoors to choose which items to send, and where they should be sent.

Skipdoors will only send things to skipdoors with a higher stockpile zone priority than themselves, and only if the filter of the target accepts the item being sent. They will always prefer sending items to the destination with the highest priority.

Skipdoors will only attempt to teleport things in a 5x5 area around itself. You can see this area in-game when you select a skipdoor. To be teleported, things must also be inside the same stockpile zone as the origin skipdoor, or they must not be in any stockpile zone. Skipdoors will never teleport items stored inside a storage building.

When things are teleported, they are placed in the stockpile zone of the chosen destination skipdoor, without a distance limit. This means that no items will be teleported if the stockpile zone has no remaining space, but also that you can make the zone as large as you need.


Development
---

To compile this mod on Windows, you will need to install the [.NET Framework 4.7.2 Developer Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472). On Linux the packages you need vary depending on your distribution of choice. Dependencies are managed using NuGet.

Compiling this project requires access to the [Vanilla Psycasts Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2842502659) mod assemblies. The csproj file of this mod is set by default to the  path to this mod on Linux when using the Steam Workshop. You may need to modify this path depending on your setup.

Contributions
---

This project encourages community involvement and contributions. Check the [CONTRIBUTING](CONTRIBUTING.md) file for details. Existing contributors can be checked in the [contributors list](https://gitlab.com/joseasoler/skipdoor-delivery/-/graphs/main).  The implementation of the core mechanics of the mod was made by [Taranchuk](https://steamcommunity.com/profiles/76561199065983477/myworkshopfiles/?appid=294100). 

[![RimWorld Mod Market discord server](https://i.imgur.com/cfoFEMA.png)](url=https://discord.gg/7befJWr9xS)

License
---

This project is licensed under the MIT license. Check the [LICENSE](LICENSE) file for details.

Acknowledgements
---

Read the [ACKNOWLEDGEMENTS](ACKNOWLEDGEMENTS.md) file for details.
