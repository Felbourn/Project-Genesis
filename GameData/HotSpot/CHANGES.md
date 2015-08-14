## v0.4.4
##### Fixes
- Support for [KIS](http://forum.kerbalspaceprogram.com/threads/113111) stackables. 

## v0.4.3
##### Fixes
- Add aggregate thermal rate metric to overlay configuration.

## v0.4.2
##### Fixes
- Fix aggregate thermal rate calculation. It was double counting radiative thermal rate.

## v0.4.1
##### Fixes
- Fix for configuration settings reverting to default after two reloads of KSP.

## v0.4.0
##### New
- Configuration settings are now persisted on scene change.

## v0.3.0
##### New
- Skin temperature added as a new metric.

##### Changes
- *Incompatible:* Configuration structure for temperature metrics has changed.
- "Temperature" in context menus abbreviated to "Temp."

##### Fixes
- Fix Internal Thermal Rate metric always being 0kW.

## v0.2.0
##### New
- Added thermal rate metrics to both overlays and context menu. These measure the change in thermal energy of a part
  over time in units of energy/time, i.e. power. There are four discrete thermal rates: internal, conductive,
  convective, and radiative. There is also a general thermal rate metric which is the combination of the previous
  four. The overlays for thermal rate are purely relative, i.e. the part with the lowest will be purple and the part
  with the highest thermal rate will be red, regardless of their absolute values.
- The screen message displayed when overlays are enabled/disabled are now customized based on the current metric and
  scheme. The screen message can also be disabled entirely in the configuration.
- Added a GUI which allows changing various options dynamically, including: context menu metrics to enable/disable,
  the temperature unit, the overlay metric, and the gradient scheme for the metric. Currently this options are not
  persisted between loads.

##### Changes
- *Incompatible:* Configuration structure has changed significantly.

## v0.1.1
##### Fixes
- Fix settings being loaded multiple times which would eventually cause the thermal overlays to fail.

## v0.1.0
##### New
- Replace thermal overlay gradient colors with more intuitive scheme.
- Add display of temperature and max temperature values to part context menu.
