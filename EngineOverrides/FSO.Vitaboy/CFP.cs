// SPDX-License-Identifier: MPL-2.0
// Based on pinned FreeSO d8a3c175; Simitone Enhanced compatibility overlay.
using FSO.Files.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Vitaboy
{
    /// <summary>
    /// Compressed Floating Point
    /// 
    /// Used to contain the animation and translation vectors for animations
    /// separately from their "head" information. This makes it easier for the
    /// TS1 content system to scan them without loading all of the animation data.
    /// </summary>
    public class CFP
    {
        public static float[] Delta = Enumerable.Range(0, 0xFD).Select(x => (float)(3.9676e-10 * (Math.Pow((double)x - 126, 3) * Math.Abs(x - 126)))).ToArray();

        public byte[] Data;

        public void Read(Stream stream)
        {
            using (var dest = new MemoryStream())
            {
                stream.CopyTo(dest);
                Data = dest.ToArray();
            }
        }

        public byte FindBestDelta(float diff)
        {
            byte closestDelta = 0;
            float closestDeltaDiff = float.PositiveInfinity;
            for (int i=0; i<256; i++)
            {
                var dd = Math.Abs(Delta[i] - diff);
                if (dd < closestDeltaDiff)
                {
                    closestDeltaDiff = dd;
                    closestDelta = (byte)i;
                }
            }
            return closestDelta;
        }

        /// <summary>
        /// Inject this CFP's translation and rotation data into the target animation. 
        /// It should contain a reference for how many translations and rotations it expects.
        /// </summary>
        /// <param name="anim">The animation to enrich.</param>
        public void EnrichAnim(Animation anim)
        {
            using (var stream = new MemoryStream(Data))
            {
                using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
                {

                    var translations = new Vector3[anim.TranslationCount];
                    var rotations = new Quaternion[anim.RotationCount];
                    //read the floating point values and give them to the anim.
                    //the anim supplies us with the count for the translation and rotation vector arrays.
                    ReadNFloats(io, translations.Count(), (i, x) => translations[i].X = -x);
                    ReadNFloats(io, translations.Count(), (i, y) => translations[i].Y = y);
                    ReadNFloats(io, translations.Count(), (i, z) => translations[i].Z = z);

                    ReadNFloats(io, rotations.Count(), (i, x) => rotations[i].X = x);
                    ReadNFloats(io, rotations.Count(), (i, y) => rotations[i].Y = -y);
                    ReadNFloats(io, rotations.Count(), (i, z) => rotations[i].Z = -z);
                    ReadNFloats(io, rotations.Count(), (i, w) => rotations[i].W = -w);
                    // Publish only a fully decoded result; a failed read must stay retryable.
                    anim.Translations = translations;
                    anim.Rotations = rotations;
                }
            }
            //Data = null;
        }

        /// <summary>
        /// Builds data for the given animation.
        /// </summary>
        /// <param name="anim"></param>
        public void CompressAnim(Animation anim)
        {
            using (var stream = new MemoryStream())
            {
                using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
                {
                    var values = new List<float>();
                    values.AddRange(anim.Translations.Select(trans => -trans.X));
                    values.AddRange(anim.Translations.Select(trans => trans.Y));
                    values.AddRange(anim.Translations.Select(trans => trans.Z));

                    values.AddRange(anim.Rotations.Select(trans => trans.X));
                    values.AddRange(anim.Rotations.Select(trans => -trans.Y));
                    values.AddRange(anim.Rotations.Select(trans => -trans.Z));
                    values.AddRange(anim.Rotations.Select(trans => -trans.W));

                    float lastValue = 0;
                    int repeatCount = 0;
                    foreach (var value in values)
                    {
                        var diff = value - lastValue;
                        var bestMatch = FindBestDelta(diff);
                        var delta = Delta[bestMatch];
                        var error = Math.Abs(diff - delta);

                        if (bestMatch == 126 && repeatCount != 65535)
                        {
                            repeatCount++;
                        }
                        else
                        {
                            if (repeatCount > 0)
                            {
                                io.WriteByte(0xFE);
                                io.WriteUInt16((ushort)repeatCount);
                            }

                            if (error > 0.0032126708614f / 2)
                            {
                                //encode as literal float
                                io.WriteByte(0xFF);
                                io.WriteFloat(value);
                                lastValue = value;
                            }
                            else
                            {
                                //encode using delta
                                io.WriteByte(bestMatch);
                                lastValue += delta;
                            }
                        }
                    }
                    if (repeatCount > 0)
                    {
                        io.WriteByte(0xFE);
                        io.WriteUInt16((ushort)repeatCount);
                    }
                }
                Data = stream.ToArray();
            }
        }

        public static void ReadNFloats(IoBuffer io, int floats, Action<int, float> output)
        {
            float lastValue = 0;
            for (int i=0; i<floats; i++)
            {
                var code = io.ReadByte();
                switch (code)
                {
                    case 0xFF:
                        lastValue = io.ReadFloat();
                        break;
                    case 0xFE:
                        //repeat count
                        var repeats = io.ReadUInt16();
                        for (int j=0; j<repeats; j++)
                        {
                            output(i++, lastValue);
                        }
                        break;
                    default:
                        lastValue += Delta[code];
                        break;
                }

                output(i, lastValue);
            }
        }
    }
}
