
![Banner](https://i.imgur.com/TLKSbJc.png "Banner")

<p align="center">
<b>A highly customizable tool for converting maps from other rhythm games into Audica maps.</b><br>

While Audica does have a decent selection of well made custom songs available, this selection pales in comparison to some of the bigger rhythm games in the world, such as osu!, which has a selection of more than **a million** custom songs. osu! features gameplay and mapping that are similar to Audica in nature, creating an excellent opportunity to convert osu maps into Audica maps. Doing so makes the entire library of osu! custom maps, which is likely to contain any more well known song you can think of, available to Audica players. There are however also many fundemental differences between osu! and Audica gameplay, which makes this conversion task far from trivial. This conversion tool enables conversion of osu! maps into Audica, performing a lot of processing of the map to ensure high quality, playable Audica maps.

The gratest innovation from the previous attempts at an osu! converter, is a highly advanced hand assignment algorithm. Additionally, the converter features better scaling, including an adaptive scaling system, streams being converted to chains, long sliders converted to sustains, some targets being selectively converted into melees, stacked notes being distributed for readability, and proper timing conversion with support for variable bpm.
</p>

## osu! Convert Example Video

<a href="https://www.youtube.com/watch?v=VQHix2SwjBE" target="_blank">![Convert Example Youtube Video](https://img.youtube.com/vi/VQHix2SwjBE/0.jpg)</a>


## Supported Games
Currently only converting maps from osu! is supported. While maps from any of osu!'s four game modes can be converted, only osu!standard maps make for decent, playable Audica maps.

## Instructions
> **Note to new players:** The converter is able to make very easy and accessible levels out of low difficulty osu! levels, allowing the converter to be used without much experience playing the game. Audica is however a very difficult rhythm game with a steep learning curve compared to most rhythm games, and is fundementally a lot harder to learn than osu! itself. The Audica-specific hand-crafted mapping of the lowest difficulty levels of the official songs are designed to be very easy and accessible to new players, in a way that the converter isn't able to. It is highly recommended to at least get comfortable with playing the official levels on standard difficulty before diving into converts, particularly if you find playing converts difficult or frustrating.

* Download the latest release from [here](https://github.com/octoberU/AudicaConverter/releases).
* Drag-and-drop one or multiple osu! songs either as `.osz` files or unpackaged songs, onto the `AudicaConverter` exe within file explorer. You can also drag-and-drop folders of osu! songs onto the converter.
* Follow the directions in the console.
* After conversion, move the resulting `.audica` file found in the automatically created `audicaFiles` folder to the games song folder. The location depends on what store/system you use, see [this page](http://www.audica.wiki/audicawiki/index.php/How_To_Get_Custom_Songs) for instructions for where to put the .audica file for your setup.

osu! beatmaps can be downloaded as `.osz` files from the [official osu! website](https://osu.ppy.sh/beatmapsets?m=0&s=any) (requires login), or from the alternative, community hosted service [bloodcat](https://bloodcat.com/osu/?q=&c=b&m=0&s=&g=&l=). If you wish to guarantee some quality of the original osu! map, it is recommended to filter on "ranked" maps. Doing so however limits your song selection a lot, so if you're looking for more specific songs you might want to include unranked maps.

>**Note!** The difficulty of the resulting Audica map mainly depends on the difficulty of the osu! map you choose to convert, and can vary from very easy to inhumanly difficult. The converter allows you to choose between the different difficulties available for each osu! map, and will give you a numeric estimate of the difficulty of the resulting Audica map on conversion. For a point of reference with the official Audica levels: beginner ≈ 2.0, standard ≈ 3.0, advanced ≈ 4.0, expert ≈ 5.0+ Audica difficulty rating. You can find osu! maps that will likely convert into appopriate Audica difficulties by filtering on osu! star difficulty rating. For example by adding "star>3 star<4" to your search on either the osu! official website or Bloodcat, you will only get maps that have at least one difficulty with a star rating between 3 and 4. Keep in mind that osu! star difficulty does not translate one-to-one to Audica difficulty rating, so it might take some trial and error to figure out what difficulties suit your preferences and skill level.

You can also associate the `.osz` file ending with the AudicaConverter exe, allowing you to convert maps by simply double-clicking the `.osz`file. To do so, right-click a `.osz` file -> "Open with...". Check the "Always use this app to open .osz files" checkbox, click "More apps" then "look for another app on this PC", navigate to and select the AudicaConverter exe. For efficient mass conversion of a large number of maps, the converter can be put into an auto conversion mode. See [this wiki page](https://github.com/octoberU/AudicaConverter/wiki/Auto-Conversion-Mode-Configuration-and-Operation) for details on setting up and running auto mode.

## Customization
The converter is highly customizable, with **a lot** of options that can be configured to adjust the converted maps to your preference. While the default configurations are optimized to provoide good conversions across a wide range of map styles, difficulties and player skill levels, a one-size-fits-all configuration isn't feasible. It is recommended to adjust settings such as the map size scaling and at what speed targets should be converted to chains based on your own preferences and experiences with the converter. See the [Converter Customization and Config Options](https://github.com/octoberU/AudicaConverter/wiki/Converter-Customization-and-Config-Options) wiki page for details on each option in the `config.json`.

## Help and Technical Support
If you need help setting up and running the converter, or run into technical issues, you can get help in the #converter-help channel in the Audica Modding Group [Discord Server](https://discord.gg/cakQUt5).

## Sharing Converts
While the Audica converter is generally able to make decent Audica maps out of most osu! maps, the nature of the original map being made for a different game means that how well the resulting convert works as an Audica map might vary significantly from map to map. In order to help each other finding the osu! maps that makes for truely good Audica converts, we encourage you to share converts that stand out as particularly enjoyable with other players in the #converted-maps channel of the Audica Modding Group [Discord Server](https://discord.gg/cakQUt5).

## Feedback and Suggestions
The AudicaConverter will see continued development after initial release, in order to further improve the conversion results. Any feedback and suggestions on the converter, including simply pointing to maps with poor conversion results, would be helpful in further improvements to the converter, and greatly appreciated. Feedback and suggestions for the converter can be posted to the #converter-feedback channel in the Audica Modding Group [Discord Server](https://discord.gg/cakQUt5).

## Internal Workings
The converter contains a lot of different, carefully designed systems for modifying and adapting osu! maps into more playable Audica maps. The most important and advanced of these are the hand assignment algorithm, which combines search/simulation techniques with mathematical modelling of strain to decide what hand each target should be required to be shot with. A more in-depth, technical description of the hand assignment algorithm can be found on [this wiki page](https://github.com/octoberU/AudicaConverter/wiki/Hand-Selection-Algorithm:-How-It-Works).

# Other Custom Songs and Mods
In addition to the converter, Audica has a selection of custom songs mapped specifically for Audica, allowing for tailored mapping and gameplay concepts such as simultaneous hand usage not utilized in converts. There is also a multitude of mods available for the game, providing significant QOL feature additions to the base game, and visual customization options such as custom arenas, guns and avatars. The go-to place to find all of these things is the Audica Modding Group [Discord Server](https://discord.gg/cakQUt5). Also see [this guide](https://bsaber.com/audica-custom-songs-mods-guide/) for an overview of available Audica mods.

# Featured articles
* https://www.windowscentral.com/add-over-one-million-songs-audica-brilliant-osu-converter  
* https://vrscout.com/news/rhythm-game-osu-vr-converter-audica/  
* https://www.androidcentral.com/add-over-one-million-songs-audica-brilliant-osu-converter  
* https://www.vrtuoluo.cn/521250.html  
