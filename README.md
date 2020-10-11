
![Banner](https://i.imgur.com/TLKSbJc.png "Banner")


<p align="center">
<b>A .NET Core based tool used for converting maps from other games into .audica files.</b><br>
The biggest innovation from the previous osu! converter is an advanced hand assignment algorithm, in addition to better scaling, streams being converted to chains, long sliders converted to sustains, stacked notes being distributed for readability and proper timing conversion with support for variable bpm.
</p>

## Usage
* Download the latest release from [here](https://github.com/octoberU/AudicaConverter/releases)
* Drag `.osz` files onto the `osutoaudica.exe`
* Follow directions in the console

## Customization
The converter is highly customizable with a lot of options that can be configured to adjust the converted maps to your preference. While the default configurations are optimized to provoide good conversions across a wide range of map styles, difficulties and player skill levels, a one-size-fits-all configuration isn't feasible. It is highly recommended to adjust settings such as the map size scaling and at what speed targets should be converted to chains based on your own needs and experiences with the converter. See the [Converter Customization and Config Options](https://github.com/octoberU/AudicaConverter/wiki/Converter-Customization-and-Config-Options) wiki page for details on each option in the config.json.

## Supported games
* osu! (all game modes, but mainly intended to be used with standard)

### Dependencies
[NAudio](https://www.nuget.org/packages/NAudio/)  
[NewtonsoftJson](https://www.nuget.org/packages/Newtonsoft.Json/)  
[System.Configuration.ConfigurationManager](https://www.nuget.org/packages/System.Configuration.ConfigurationManager/)
[Audica .NET Tools](https://github.com/octoberU/Audica-.NET-Tools)  
