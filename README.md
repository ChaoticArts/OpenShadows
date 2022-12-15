# OpenShadows
Engine reimplementation of Shadows over Riva.

## About
This project tries to reimplement the executables of **Realms of Arkania III: Shadows over Riva**. It's main objectives are:

* historical preservation of the game by enabling it to run on modern systems
* having fun reverse engineering the DOS-based game and its file formats
* getting more and more into graphics and game programming


## Prototype Screenshots
> Note: Not everything is rendered correctly yet. The screenshots were taken using an OBJ viewer from the exported map data.

![Market](/Screenshots/market.jpg?raw=true)
![Riva](/Screenshots/riva01.jpg?raw=true)
![Riva2](/Screenshots/riva02.jpg?raw=true)
![Tower](/Screenshots/magetower.jpg?raw=true)
![Outdoor](/Screenshots/env.jpg?raw=true)

## Technology
The game will be developed using **.NET Core** in combination with [Veldrid](https://www.github.com/mellinoe/Veldrid). This tech-stack ensures good platform independence while simultaneously granting rapid development speed, suitable performance and access to modern graphics.


## Work in progress / roadmap
This project is in the *very* early stages. There is still a long way to go before there's anything playable. The initial focus is on understanding the data formats and converting them to data that can be used by modern systems (images, sounds, music, ...).

### Understand, identify and parse data files
:white_check_mark:   `.ALF` Game archive files <br>
:white_large_square: `.3DM` 3d map <br>
:white_large_square: `.DSC` 3d map animation data <br>
:white_large_square: `.NEI` 3d map portal data (???) <br>
:white_check_mark:   `.PAL` 3d map texture palette <br>
:white_large_square: `.PPD` 3d map sky definition (???) <br>
:white_large_square: `.TAB` 3d map shading information (???) <br>
:white_check_mark:   `.PIX` 3d map texture <br>
:white_check_mark:   `.AIF` image <br>
:white_check_mark:   `.NVF` image set <br>
:white_check_mark:   `.ACE` animated sprites <br>
:white_large_square: `.BOB` animated screens <br>
:white_large_square: `.PCX` image (ZSoft PC Paintbrush) <br>
:white_large_square: `.AAF` cutscene definition <br>
:white_large_square: `.SMK` video (Smacker) <br>
:white_large_square: `.ASF` audio (raw PCM unsigned 8-bit, mono 22050 Hz) with ASF header <br>
:white_large_square: `.RAW` audio (raw PCM unsigned 8-bit, mono 22050 Hz) <br>
:white_large_square: `.HMI` audio (Human Machine Interfaces format) <br>
:white_check_mark:   `.LXT` text definition <br>
:white_large_square: `.XDF` dialogue definition <br>
:white_large_square: `.DAT` different types of gamedata <br>
:white_large_square: `.NPC` joinable npc data <br>
:white_large_square: `.HTT` some form of hyper text <br>
:white_large_square: `.ANN` map annotations for minimaps <br>
:white_large_square: `.APA` (???) <br>
:white_large_square: `.MSK` (???) <br>
:white_large_square: `.MST` (???) <br>
:white_large_square: `.LST` (???) <br>
:white_large_square: `.MOF` (???) <br>
:white_large_square: `.MOV` (???) <br>
:white_large_square: `.OFF` (??? probably positioning information of objects on battle screen) <br>


### Rendering
:white_large_square: Render game GUI <br>
:white_large_square: Render 3D map loaded from `.3dm`, including textures and animations <br>
:white_large_square: Render isometric battle maps <br>
:white_large_square: Render video sequences <br>

### Game logic
:white_large_square: Character and party management <br>
:white_large_square: Shops and inventory management <br>
:white_large_square: Visiting houses and npc dialogues <br>
:white_large_square: Level exploration <br>
:white_large_square: Quest state and system <br>
:white_large_square: Battles <br>

### Platforms
:white_large_square: Windows <br>
:white_large_square: Linux <br>


## Community
This project itself has no distinct community (yet). But there are many folks with a great understanding of the inner workings of the original game at [Crystals-DSA-Foren](http://www.crystals-dsa-foren.de), even more so at the [Technische Werkstatt](http://www.crystals-dsa-foren.de/showthread.php?tid=700).


## Acknowledgements
Some people I like to mention (in no particular order):

* Crystal
* HenneNWH
* Hendrik
* Lippens die Ente
* wetzer
* Obi-Wahn
* helios

You guys and your enthusiasm rocks!

Special acknowledgements go out the the people of [OpenSAGE](https://github.com/OpenSAGE/OpenSAGE). I took the liberty of being inspired by their structure and -- obviously -- the format of their README.MD ;-)


## Similar projects
This project has a similar goal for the first installation of the Realms of Arkania trilogy:

[Bright-Eyes](https://github.com/Henne/Bright-Eyes)


## Legal disclaimers
This project is not affiliated with Attic and Fanpro in any way. This reimplementation does not provide access to the original game content. To use this reimplementation, a legal copy of **Shadows over Riva** is absolutely necessary.

No harm intended!
