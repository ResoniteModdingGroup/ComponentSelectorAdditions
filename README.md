Component Selector Additions
============================

A [MonkeyLoader](https://github.com/MonkeyModdingTroop/MonkeyLoader) mod for [Resonite](https://resonite.com/) that overhauls the Component Selector / Protoflux Node Selector to have a search, as well as favorites and recents categories.

## Install
First, make sure you've installed MonkeyLoader and the necessary GamePacks - combined releases can be found on the page of the Resonite GamePack here: https://github.com/ResoniteModdingGroup/MonkeyLoader.GamePacks.Resonite/releases/

Then all you have to do is placing the provided `ComponentSelectorAdditions.nupkg` into your `Resonite/MonkeyLoader/Mods/` folder.  

## Features

* Makes the UI construction and listing of elements modular by exposing a bunch of events which this mod uses for the other features - but can be used by others too
* Shows the current category / generic type path at the bottom
* Adds 'Favorites' categories to the Component Selector and Nodebrowser roots
  * Categories and (generic) Components / Nodes can be favorited
  * Favoriting multiple components from a group will make the group show up
  * Use in combination with Recents to favorite specific custom versions of generic components
  * Favorite Categories or Components / Nodes can be sorted to the top of the list
* Adds 'Recents' categories to the Component Selector and Nodebrowser roots
  * Tracks recently used components and nodes separately
  * For generic components, the generic and/or concrete versions can be saved
  * Maximum number can be adjusted
* Fixes the Back and Cancel buttons to the top / bottom, outside of the scrollable area
* Adds a searchbar at the top of Component Selectors / Nodebrowsers
  * Works for anyone in the Session!
  * Each separate word is searched by contains - more matches will put a result higher
    * E.g. searching "value gradient" will find everything matching either "value" or "gradient" - with things matching both coming first
  * Exclude certain categories from being searched into from outside of them
    * By default this is the ProtoFlux category
  * Excluded categories can be added programmatically too (i.e. the Favorites and Recents categories)
  * Search inside the categories works as expected
  * Shouldn't slow anything else down as it doesn't use more reflection than the normal selector


## Known Incompatibilities

Obviously not compatible with CherryPick.
