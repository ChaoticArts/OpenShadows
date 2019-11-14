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
* [ ] `.ALF` Game archive files
* [ ] `.3DM` 3d map
* [ ] `.DSC` 3d map animation data
* [ ] `.NEI` 3d map portal data (???)
* [ ] `.PAL` 3d map texture palette
* [ ] `.PPD` 3d map sky definition (???)
* [ ] `.TAB` 3d map shading information (???)
* [ ] `.PIX` 3d map texture
* [ ] `.AIF` image
* [ ] `.NVF` image set
* [ ] `.ACE` animated sprites
* [ ] `.BOB` animated screens
* [ ] `.PCX` image (ZSoft PC Paintbrush)
* [ ] `.AAF` cutscene definition
* [ ] `.SMK` video (Smacker)
* [ ] `.ASF` audio (raw PCM unsigned 8-bit, mono 22050 Hz) with ASF header
* [ ] `.RAW` audio (raw PCM unsigned 8-bit, mono 22050 Hz)
* [ ] `.HMI` audio (Human Machine Interfaces format)
* [ ] `.LXT` text definition
* [ ] `.XDF` dialogue definition
* [ ] `.DAT` different types of gamedata
* [ ] `.NPC` joinable npc data
* [ ] `.HTT` some form of hyper text
* [ ] `.ANN` map annotations for minimaps
* [ ] `.APA` (???)
* [ ] `.MSK` (???)
* [ ] `.MST` (???)
* [ ] `.LST` (???)
* [ ] `.MOF` (???)
* [ ] `.MOV` (???)
* [ ] `.OFF` (???)


### Rendering
* [ ] Render game GUI
* [ ] Render 3D map loaded from `.3dm`, including textures and animations
* [ ] Render isometric battle maps
* [ ] Render video sequences

### Game logic
* [ ] Character and party management
* [ ] Shops and inventory management
* [ ] Visiting houses and npc dialogues
* [ ] Level exploration
* [ ] Quest state and system
* [ ] Battles

### Platforms
* [ ] Windows
* [ ] Linux


## Community
This project itself has no distinct community (yet). But there are many folks with a great understanding of the inner workings of the original game at [Crystals-DSA-Foren](http://www.crystals-dsa-foren.de), even more so at the [Technische Werkstatt](http://www.crystals-dsa-foren.de/showthread.php?tid=700).


## Acknowledgements
Some people I like to mention (in no particular order):

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
