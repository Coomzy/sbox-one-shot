# About S&bux API

_s&bux is a parody of in-game currencies and can't be bought with real world money._

You get 25 s&bux for free every day you play a supported gamemode. 😃

# How to use

Create a game pass resource.

https://github.com/s8bux/sbux-api/assets/91832803/e3da438a-9a28-4e34-a63c-821c6a40c1c5

![image](https://github.com/s8bux/sbux-api/assets/91832803/588629d3-545c-4449-8ed5-897180962da5)

```c#
// Check if a player owns the game pass.

if ( Monetization.Has( gamePass ) )
{
    // Do things...
}
```

![image](https://github.com/s8bux/sbux-api/assets/91832803/588629d3-545c-4449-8ed5-897180962da5)

```c#
// Prompts the player to purchase the game pass.
// True if the game pass was bought.
// A game pass can be bought multiple times.

if ( await Monetization.Purchase( gamePass ) )
{
    // Do things...
}
``` 
