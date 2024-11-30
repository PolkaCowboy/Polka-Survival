# Polka-Survival
Custom survival tuning for RDR2. These changes I've written entirely for myself engaged with antics outside of the story mode for my [YouTube Channel](https://www.youtube.com/@PolkaCowboy). 

This mod is currently written without any considerations for story mode. Use in story mode run the following risks:
- Randomly dying during cut scenes
- Breaking any mission that requires Eagle Eye
- Breaking any mission that requires Dead Eye
- Arthur/John getting really fat

# What this mod does

My goal here is to make using consumables to keep the stat cores up more critical by making the side effects of not keeping them full more dire.

## Health Core

Now only regenerates outer ring hit points if it is full above a certain percent. (Currently set to 60%) The rate which it heals also ramps up as it gets closer to 100% fullness, with barely a trickle at the lower end.

If the core goes red it will start to actively hurt the hit points. Again, the rate of pain ramps up the closer it gets to zero. It is fatal if this mechanic drains all the hitpoints.

If white, but below the upper limit, no healing happens.

## Stamina Core

If it falls into red it will also start to drain the health core. 
 
## Dead Eye Core

I always felt like Dead Eye is a bit too powerful for the amount of uptime it can have.

Draining the core won’t affect anything else, but it now costs a lot more deadeye to activate and drains the ability about twice as fast when active

## Eagle Eye

Eagle eye is disabled. 

## Water

I also added some crude hypothermia emulation.

Splashing through water is fine, but spend too much time in it and it will start to decay the health core. Swimming or getting totally dunked into water will add a stronger decay to the health core.

These health core decays will last for a while after leaving the water.

The intensity of the decay is influenced by the current temperature. So wading through the swamps will be trivial, if impactful at all. But falling into a lake in Ambarino can result in a very bad time.

# Requirements
[ScriptHookRDR2DotNet-V2](https://github.com/Halen84/ScriptHookRDR2DotNet-V2)

# Installation

1. Install [ScriptHookRDR2DotNet-V2](https://github.com/Halen84/ScriptHookRDR2DotNet-V2)
2. Dowload [the latest release](https://github.com/PolkaCowboy/Polka-Survival/releases) and extract the contents of the zip file into your `RedDeadRedemption2` install folder. the `.dll` and `.ini` need to end up in `../RedDeadRedemption2/scripts/`

That should be it! You should see a message pop up sayinf **Polka Survival Activated** if everything is correct. 

Many of the values can be tweaks and feature enabled/disabled via the .ini file. If you make a change while the game is running you can reload the script with the `ins` key without restarting. This key is a config within ScriptHookRDR2DotNet-V2's .ini and can be changed to your liking there.
