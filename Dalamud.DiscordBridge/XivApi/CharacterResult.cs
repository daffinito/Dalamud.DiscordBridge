using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.DiscordBridge.XivApi
{
    public class CharacterResult
    {
        public required string AvatarUrl { get; set; }
        public required string LodestoneId { get; set; }
    }
}
