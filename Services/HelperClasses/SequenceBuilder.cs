
using System.Buffers;
using System.Diagnostics;

namespace firstgptapp.Services.HelperClasses
{
    public class SequenceBuilder<T>
    {
        Segment _first;
        Segment _last;

        /// <summary>
        /// Appends a memory segment to the internal linked list structure.
        /// </summary>
        public void Append(ReadOnlyMemory<T> data)
        {
            if (_first == null)
            {
                Debug.Assert(_last == null);
                _first = new Segment(data);
                _last = _first;
            }
            else
            {
                _last = _last!.Append(data);
            }
        }

        /// <summary>
        /// Constructs and returns a ReadOnlySequence<T> made from the accumulated segments.
        /// </summary>
        public ReadOnlySequence<T> Build()
        {
            if (_first == null)
            {
                Debug.Assert(_last == null);
                return ReadOnlySequence<T>.Empty;
            }

            if (_first == _last)
            {
                Debug.Assert(_first.Next == null);
                return new ReadOnlySequence<T>(_first.Memory);
            }

            return new ReadOnlySequence<T>(_first, 0, _last!, _last!.Memory.Length);
        }

        /// <summary>
        /// A custom implementation of ReadOnlySequenceSegment<T>. 
        /// It holds one memory block and points to the next one, 
        /// allowing the entire sequence to be reconstructed as a stream.
        /// </summary>
        private sealed class Segment : ReadOnlySequenceSegment<T>
        {
            public Segment(ReadOnlyMemory<T> items) : this(items, 0)
            {
            }

            private Segment(ReadOnlyMemory<T> items, long runningIndex)
            {
                Debug.Assert(runningIndex >= 0);
                Memory = items;
                RunningIndex = runningIndex;
            }

            public Segment Append(ReadOnlyMemory<T> items)
            {
                long runningIndex;
                checked { runningIndex = RunningIndex + Memory.Length; }
                Segment segment = new(items, runningIndex);
                Next = segment;
                return segment;
            }
        }
    }
}
