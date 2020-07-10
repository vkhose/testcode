/**
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *     https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.IO;
using System.Linq;
using NUnit.Framework;
using Avro.IO;
using Avro.Specific;
using System.Collections.Generic;
using com.parent.@event;

namespace Avro.Test
{
    [TestFixture]
    class SpecificTestsToReproduceError
    {
        [Test]
        public void TestNamespaceWithCSharpKeyword()
        {
            var srcRecord = new Parent
            {
                children = new List<Child>
                {
                    new Child
                    {
                        name = "test"
                    },
                    new Child
                    {
                        name = "test"
                    }
                }
            };
            var stream = serialize(Parent._SCHEMA, srcRecord);
            var dstRecord = deserialize<Parent>(stream,
                Parent._SCHEMA, Parent._SCHEMA);

            Assert.NotNull(dstRecord);
        }

        private static S deserialize<S>(Stream ms, Schema ws, Schema rs) where S : class, ISpecificRecord
        {
            long initialPos = ms.Position;
            var r = new SpecificReader<S>(ws, rs);
            Decoder d = new BinaryDecoder(ms);
            S output = r.Read(null, d);
            Assert.AreEqual(ms.Length, ms.Position); // Ensure we have read everything.
            checkAlternateDeserializers(output, ms, initialPos, ws, rs);
            return output;
        }

        private static void checkAlternateDeserializers<S>(S expected, Stream input, long startPos, Schema ws,
            Schema rs) where S : class, ISpecificRecord
        {
            input.Position = startPos;
            var reader = new SpecificDatumReader<S>(ws, rs);
            Decoder d = new BinaryDecoder(input);
            S output = reader.Read(null, d);
            Assert.AreEqual(input.Length, input.Position); // Ensure we have read everything.
            //AssertSpecificRecordEqual(expected, output);
        }

        private static Stream serialize<T>(Schema ws, T actual)
        {
            var ms = new MemoryStream();
            Encoder e = new BinaryEncoder(ms);
            var w = new SpecificWriter<T>(ws);
            w.Write(actual, e);
            ms.Flush();
            ms.Position = 0;
            checkAlternateSerializers(ms.ToArray(), actual, ws);
            return ms;
        }

        private static void checkAlternateSerializers<T>(byte[] expected, T value, Schema ws)
        {
            var ms = new MemoryStream();
            var writer = new SpecificDatumWriter<T>(ws);
            var e = new BinaryEncoder(ms);
            writer.Write(value, e);
            var output = ms.ToArray();

            Assert.AreEqual(expected.Length, output.Length);
            Assert.True(expected.SequenceEqual(output));
        }
    }
}
