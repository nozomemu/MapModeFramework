# Map Mode Framework
![](https://raw.githubusercontent.com/nozomemu/MapModeFramework/main/About/Preview.png)
Map Mode Framework is a code library/framework mod containing classes and utilities for creating map modes in RimWorld. This attempts to simulate switchable map modes on the world map through shared WorldLayers, minimizing the amount of WorldLayers needed to render and regenerate.

## Features
- **Dynamic Map Modes**: Seamlessly switch between different map modes and adjust draw settings on the fly.
- **Efficient Rendering**: Only the `WorldLayer_MapMode` corresponding to the current `MapMode` is rendered and can regenerate, optimizing performance especially on high coverage worlds with lots of tiles.
- **Flexible Map Mode Creation**: Adding a new map mode requires only a `MapModeDef` with a corresponding `MapMode` subclass, which contains overridable methods for determining the Material, tile label, and tooltip to render on a tile. Though `WorldLayer_MapMode_Terrain` already covers all tiles on the world map, you can make a subclass of `WorldLayer_MapMode` if you seek further customization.
- **Regions**: Instead of drawing by tile, `MapMode_GenericRegion<T>` allows you to specify a collection of tiles to draw over, similar to areas or regions on the world map.
