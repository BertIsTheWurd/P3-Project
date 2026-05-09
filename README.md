# Ministry of Truth

Ministry of Truth is a card game where you must create paths by laying down cards in order to find "evidence".

You work for _The Ministry_ who you report your findings to, but you do not know what this evidence entails or what The Ministry will do with it.

<img width="2560" height="1440" alt="image" src="https://github.com/user-attachments/assets/c0686065-7654-4641-90f9-042ec607b8b5" />

But while you play, sinister forces are at play.

You may find that if you look away, some of your cards suddenly disappear.

There seems to be someone giving you obstacles and obstructing your work, but you do not know who.

But watch out, if you fail your tasks, consequences will follow.

## Specifications

This game has been built using Unity 6000.2.10f1.

Attached in this repository is a folder containint the Unity project and a Python script which is supposed to run while playing the game.

The Python script contains a gaze detection program which uses camera data to detect whether or not the player is looking at their screen, and then subsequently sends that data to Unity.

If the player looks away from their screen, someone might take away their cards.
