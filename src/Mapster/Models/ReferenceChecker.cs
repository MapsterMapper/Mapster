using System;
using System.Runtime.CompilerServices;

namespace Mapster.Models
{
    internal class ReferenceChecker
    {
        public static readonly ReferenceChecker Default = new ReferenceChecker();

        private static readonly int[] _emptyHash = new int[0];
        private static readonly object[] _emptyObject = new object[0];

        private readonly int[] _hashes;
        private readonly object[] _objects;
        private readonly object[] _results;
        private readonly int _pos;

        public ReferenceChecker()
        {
            _hashes = _emptyHash;
            _objects = _emptyObject;
            _results = _emptyObject;
            _pos = -1;
        }

        private ReferenceChecker(int[] hashes, object[] objects, object[] results, int pos)
        {
            _hashes = hashes;
            _objects = objects;
            _results = results;
            _pos = pos;
        }

        public bool IsCircular => _hashes == null;

        public object Result
        {
            get { return _results[_pos]; }
        }

        public ReferenceChecker Add(object obj, object result)
        {
            var hash = RuntimeHelpers.GetHashCode(obj);
            var pos = Array.BinarySearch(_hashes, hash);
            if (pos >= 0)
            {
                for (var down = pos; down >= 0 && _hashes[down] == hash; down--)
                {
                    if (object.ReferenceEquals(_objects[down], obj))
                        return new ReferenceChecker(null, null, new[] {_objects[down]}, 0);
                }
                for (var up = pos + 1; up < _hashes.Length && _hashes[up] == hash; up++)
                {
                    if (object.ReferenceEquals(_objects[up], obj))
                        return new ReferenceChecker(null, null, new[] {_objects[up]}, 0);
                }
            }
            else
            {
                pos = ~pos;
            }

            var newHashes = new int[_hashes.Length + 1];
            var newObjects = new object[_hashes.Length + 1];
            var newResults = new object[_hashes.Length + 1];
            if (pos > 0)
            {
                Array.Copy(_hashes, 0, newHashes, 0, pos);
                Array.Copy(_objects, 0, newObjects, 0, pos);
                Array.Copy(_results, 0, newResults, 0, pos);
            }
            newHashes[pos] = hash;
            newObjects[pos] = obj;
            newResults[pos] = result;
            var remain = _hashes.Length - pos;
            if (remain > 0)
            {
                Array.Copy(_hashes, pos, newHashes, pos + 1, remain);
                Array.Copy(_objects, pos, newObjects, pos + 1, remain);
                Array.Copy(_results, pos, newResults, pos + 1, remain);
            }
            return new ReferenceChecker(newHashes, newObjects, newResults, pos);
        }
    }
}
