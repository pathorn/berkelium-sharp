using System;

namespace BerkeliumXNATest {
    static class Program {
        static void Main (string[] args) {
            using (BerkeliumTestGame game = new BerkeliumTestGame()) {
                game.Run();
            }
        }
    }
}

