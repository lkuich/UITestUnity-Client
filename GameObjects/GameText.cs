using System;

namespace Xamarin.GameTestServer
{
    [GameObjectType("Text")]
    public class GameText : GameElement
    {
        public string Text { get; set; }
    }
}