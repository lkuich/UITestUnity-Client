using System.Drawing;

namespace Xamarin.GameTestServer
{
    [GameObjectType("GameObject")]
    public class GameElement
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
        public string Parent { get; set; }
        public string[] Children { get; set; }

        public PointF Location { get; set; }
        public RectangleF Rectangle { get; set; }
        public bool? IsOnScreen { get; set; }
    }
}