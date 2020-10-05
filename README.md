# OpenShadows
Engine reimplementation of Shadows over Riva.

## About
This project tries to reimplement the executables of **Realms of Arkania III: Shadows over Riva**. It's main objectives are:

* historical preservation of the game by enabling it to run on modern systems
* having fun reverse engineering the DOS-based game and its file formats
* getting more and more into graphics and game programming


## Technology
The game will be developed using **.NET Core** in combination with [Veldrid](https://www.github.com/mellinoe/Veldrid). This tech-stack ensures good platform independence while simultaneously granting rapid development speed, suitable performance and access to modern graphics.


## Work in progress / roadmap
This project is in the *very* early stages. There is still a long way to go before there's anything playable. The initial focus is on understanding the data formats and converting them to data that can be used by modern systems (images, sounds, music, ...).

### Understand, identify and parse data files
* :white_check_mark:   `.ALF` Game archive files
* :white_large_square: `.3DM` 3d map
* :white_large_square: `.DSC` 3d map animation data
* :white_large_square: `.NEI` 3d map portal data (???)
* :white_check_mark:   `.PAL` 3d map texture palette
* :white_large_square: `.PPD` 3d map sky definition (???)
* :white_large_square: `.TAB` 3d map shading information (???)
* :white_check_mark:   `.PIX` 3d map texture
* :white_check_mark:   `.AIF` image
* :white_large_square: `.NVF` image set
* :white_large_square: `.ACE` animated sprites
* :white_large_square: `.BOB` animated screens
* :white_large_square: `.PCX` image (ZSoft PC Paintbrush)
* :white_large_square: `.AAF` cutscene definition
* :white_large_square: `.SMK` video (Smacker)
* :white_large_square: `.ASF` audio (raw PCM unsigned 8-bit, mono 22050 Hz) with ASF header
* :white_large_square: `.RAW` audio (raw PCM unsigned 8-bit, mono 22050 Hz)
* :white_large_square: `.HMI` audio (Human Machine Interfaces format)
* :white_check_mark:   `.LXT` text definition
* :white_large_square: `.XDF` dialogue definition
* :white_large_square: `.DAT` different types of gamedata
* :white_large_square: `.NPC` joinable npc data
* :white_large_square: `.HTT` some form of hyper text
* :white_large_square: `.ANN` map annotations for minimaps
* :white_large_square: `.APA` (???)
* :white_large_square: `.MSK` (???)
* :white_large_square: `.MST` (???)
* :white_large_square: `.LST` (???)
* :white_large_square: `.MOF` (???)
* :white_large_square: `.MOV` (???)
* :white_large_square: `.OFF` (???)


### Rendering
* :white_large_square: Render game GUI
* :white_large_square: Render 3D map loaded from `.3dm`, including textures and animations
* :white_large_square: Render isometric battle maps
* :white_large_square: Render video sequences

### Game logic
* :white_large_square: Character and party management
* :white_large_square: Shops and inventory management
* :white_large_square: Visiting houses and npc dialogues
* :white_large_square: Level exploration
* :white_large_square: Quest state and system
* :white_large_square: Battles

### Platforms
* :white_large_square: Windows
* :white_large_square: Linux


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
