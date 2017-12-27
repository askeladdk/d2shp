/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 * 
 * Modified from OpenRA source code.
 */
namespace D2SHP
{
    public static class Format2
    {
        public static void Decode(byte[] src, byte[] dst, int dstpos)
        {
            for(var i = 0; i < src.Length;)
            {
                var cmd = src[i++];
                if (cmd == 0)
                {
                    var count = src[i++];
                    while (count-- > 0)
                        dst[dstpos++] = 0;
                }
                else
                    dst[dstpos++] = cmd;
            }
        }
    }
}
