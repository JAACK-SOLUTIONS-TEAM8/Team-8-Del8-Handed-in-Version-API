﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Louman.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return Ok();
        }
    }
}