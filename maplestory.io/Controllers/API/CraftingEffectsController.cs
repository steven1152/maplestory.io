using Microsoft.AspNetCore.Mvc;
using PKG1;
using maplestory.io.Data.Images;

namespace maplestory.io.Controllers.API
{
    [Route("api/crafting/effects")]
    public class CraftingEffectsController : APIController
    {
        [Route("")]
        [HttpGet]
        public IActionResult GetListing() => Json(CraftingEffectFactory.EffectList());

        [Route("{effectName}")]
        [HttpGet]
        public IActionResult GetEffect(string effectName) => Json(CraftingEffectFactory.GetEffect(effectName));
    }
}