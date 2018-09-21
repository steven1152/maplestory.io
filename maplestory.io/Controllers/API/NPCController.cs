using maplestory.io.Data.Images;
using maplestory.io.Data.NPC;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace maplestory.io.Controllers
{
    [Route("api/{region}/{version}/npc")]
    public class NPCController : APIController
    {
        [Route("")]
        [HttpGet]
        public IActionResult List([FromQuery] int startAt = 0, [FromQuery]int count = int.MaxValue, [FromQuery]string searchFor = "") => Json(NPCFactory.GetNPCs(startAt, count, searchFor));

        [Route("{npcId}")]
        [HttpGet]
        public IActionResult GetNPC(int npcId) => Json(NPCFactory.GetNPC(npcId));

        [Route("{npcId}/icon")]
        [HttpGet]
        public IActionResult GetFrame(int npcId)
        {
            NPC npcData = NPCFactory.GetNPC(npcId);
            if (!npcData.Framebooks.ContainsKey("stand")) return NotFound();

            FrameBook standing = npcData.GetFrameBook("stand").First();
            if (standing == null) return NotFound();

            Frame firstFrame = standing.frames.First();
            if (firstFrame == null || firstFrame.Image == null) return NotFound();

            return File(firstFrame.Image.ImageToByte(Request), "image/png");
        }

        [Route("{npcId}/name")]
        [HttpGet]
        public IActionResult GetName(int npcId)
        {
            NPC npcData = NPCFactory.GetNPC(npcId);
            return Json(new
            {
                Name = npcData.Name,
                Id = npcData.Id
            });
        }

        [Route("{npcId}/render/{framebook}/{frame?}")]
        [HttpGet]
        public IActionResult Render(int npcId, string framebook, int frame = 0)
        {
            NPC npcData = NPCFactory.GetNPC(npcId);

            FrameBook standing = npcData.GetFrameBook(framebook).First();
            if (standing == null) return NotFound();

            Frame firstFrame = standing.frames.ElementAt(frame % standing.frames.Count());
            if (firstFrame == null || firstFrame.Image == null) return NotFound();

            return File(firstFrame.Image.ImageToByte(Request), "image/png");
        }
    }
}