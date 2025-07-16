# Brick Shmup (Working Title)

A modern reimagining of the classic brick breaker, Brick Shmup blends arcade-style paddle mechanics with the dynamic structure of a shoot-'em-up. Built from scratch in Unity, the game features a custom, grid-based architecture designed to support a wide variety of gameplay modes and systemic interactions. The project serves as a technical and design platform for exploring meaningful player decision-making through mechanical depth, flexible core systems, and experimental level formats, from classic single-screen challenge layouts to scrolling shooter hybrids and multi-screen pinball-inspired stages.


# Core Systems:

Custom Brick Grid Architecture: A modular grid separates brick logic from presentation. Bricks are managed via a central BrickGrid system, with rendering handled independently by BrickView components. This enables efficient simulation, clean separation of concerns, and rapid iteration on visuals or behavior.

Brick State Management: Bricks transition through defined states (e.g. Normal → Cracked → Destroyed), with support for unbreakables, multi-hit bricks, chain reactions, and bricks that trigger level-specific effects. Each transition can trigger both visual and systemic responses.

Extensible Paddle & Ball Logic:
The paddle supports a minimalist movement base, layered with modular mechanics like:
- Paddle Punch: A time-based smacking ability that sends the ball back at variable speeds and angles based on timing and positioning, giving players a high-risk/high-reward control tool.
- Magnet Pull: A ranged ability that slows or reverses the ball’s momentum, allowing players to tactically reposition or stabilize chaotic situations.

Ball physics and collision systems are built for precision and flexibility, with future expansion planned for spin mechanics, angle-based targeting, or velocity modifiers.


# Key Features:

Emergent Interaction Space: Though rooted in a familiar genre, the game is structured to allow for mechanical layering: power-ups, spatial hazards, dynamic brick types, scrolling levels, and combat interactions can be added modularly, supporting moment-to-moment decisions and diverse playstyles.

Stage Variety Through Core Structure:
Level design explores the expressive potential of the base mechanics by introducing a range of layout types:
- Traditional static layouts for tutorials and skill tests;
- Vertically scrolling stages focusing on score attack, enemy flight patterns, and bullet avoidance/reflection;
- Multi-screen pinball-style arenas;
- Experimental circular levels.


# Design & Programming Goals:

Expand Player Agency Through Systems: Rather than relying on content volume, depth is created through dynamic decisions: players control ball angle, timing, positioning, and use risk/reward evaluations in real time.

Systemic Design for Level Archetypes: Stage types are mechanically differentiated, focusing on a different design axis such as precision, endurance, and/or spatial awareness.

Scalable & Maintainable Architecture: Code is written for extensibility, systems are decoupled, testable, and agnostic to rendering. Brick types, ball behaviors, and stage modifiers can be introduced with minimal refactoring.


# Technologies & Architecture:
Unity (C#) – Custom gameplay systems;
Modular brick grid – Logic/view separation;
Serialized level formats for fast prototyping.


# Current Progress & Next Steps:
Core gameplay loop functional and tested;
Adding responsive feedback systems (e.g. VFX, audio, screen shake);
Expanding brick types and interactions;
Prototyping stage archetypes and alternate game modes;
Investigating modular tools for in-editor level design.
