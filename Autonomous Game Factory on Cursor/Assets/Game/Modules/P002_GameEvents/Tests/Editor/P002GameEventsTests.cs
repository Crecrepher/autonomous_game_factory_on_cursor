using NUnit.Framework;
using UnityEngine;

namespace Game
{
    public class P002GameEventsTests
    {
        [Test]
        public void Create_ReturnsNonNull()
        {
            P002GameEventsConfig config = ScriptableObject.CreateInstance<P002GameEventsConfig>();
            IP002GameEvents runtime = P002GameEventsFactory.Create(config);
            Assert.IsNotNull(runtime);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void RaisePuzzleMatched_DoesNotThrow()
        {
            P002GameEventsConfig config = ScriptableObject.CreateInstance<P002GameEventsConfig>();
            IP002GameEvents runtime = P002GameEventsFactory.Create(config);
            Assert.DoesNotThrow(() => runtime.RaisePuzzleMatched(0, 0));
            Assert.DoesNotThrow(() => runtime.RaisePuzzleMatched(2, 3));
            Object.DestroyImmediate(config);
        }
    }
}
