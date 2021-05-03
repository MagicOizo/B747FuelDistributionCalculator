# B747 Fuel Distribution Calculator
A Fuel Distribution Calculator for Boeing 747 sim pilots (mainly X-Plane 11).
## Supported Models
Currently there are 4 X-Plane 11 models of the Boeing 747 supported, because I own them:
- Laminar B747-400 (X-Plane 11 default)
- SSG B747-8 Aniversary Edition V2 (Passenger and Freight Version)
- mSparks B747-400 (Mod of the default B747-400)
## Why did I do this:
Supercritical Simulation Groups B747-8 has a EFB that does not properly fill up the eight tanks of the arcraft following the rules stated in the Flight Crew Operation Manaual (FCOM). It does not full up the tanks beginning with the main tanks and ending with the center and stabelizer tanks. So I used the table provided and calculated the load by myself. But to configure it with the X-Plane 11 Load & Balance Management is not very easy.
For a other project I experimented with NASA's X-Plane Connect plugin and so I decided to combine both. A better calculation with the option to automaticaly set it in the plane.
Because I also owned the mSparks B747-400 and of course the default B747-400 by Laminar I just added them as an option to the calculator too.
So now I can simply put in the fuel calculation from my flight planing tool (simbrief) and then set it to the correct tanks directly to the simulator by one single click.
Because I also fly the 747 in FSEconomy, I liked to have a simple way to calculate fuel loads in gallons of JetA, because this is the way FSEconomy works. I wanted to get the kg from my Simbrief flight plan and calculate the right amount of gallons to refuel the plane in FSEconmy and still get the right loading.
## Prerequisites
- .NET-Framework v4.7.2 [Download here](https://dotnet.microsoft.com/download/dotnet-framework/net472)
- NASA's XPlaneConnect Plug-In (only if you want to set the calculated values directly to the sim) [Project GitHub](https://github.com/nasa/XPlaneConnect)
## Features
- Calculation is always done in metric (kg).
- Input of target load in metric (ton or 1000th of kgs), imperial (lbs) and imperial (gal in the way FSEconomy calculates).
- Output of the loads per tank in metric (ton or 1000th of kgs), imperial (lbs) and imperial (gal in the way FSEconomy calculates).
- Visualization of the fuel distribution
- Consideration of the difference between fuel distribution of a B747-400 and a B747-8
- Get curret load from sim (X-Plane 11)
- Set calculated load to sim (X-Plane 11)
## Version History
### Changelog Version 0.9.1 Beta
- Bugfixes
- Addition of a visualization of the fuel distribution
- Consideration of the difference between fuel distribution of a B747-400 and a B747-8
### Version 0.9 Beta
Initial publication
## Donate
If you appreciate my work and want to donate some money. Feel free to do so: https://paypal.me/MagicOizo
