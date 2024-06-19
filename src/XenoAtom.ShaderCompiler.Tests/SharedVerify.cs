// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.ShaderCompiler.Tests;

public static class SharedVerify
{
    public static VerifySettings CreateVerifySettings()
    {
        var settings = new VerifySettings();
        settings.UseDirectory("Verified");
        settings.DisableDiff();
        return settings;
    }
}