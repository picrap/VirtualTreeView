// VirtualTreeView - a TreeView that *actually* allows virtualization
// https://github.com/picrap/VirtualTreeView

namespace VirtualTreeView.Collection.Reader
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Specialized reader for <see cref="IEnumerable"/>
    /// </summary>
    public class EnumerableCollectionReader : CollectionReader
    {
        private readonly IEnumerable _enumerable;

        private IEnumerator _enumerator;
        private int _enumeratedCount;

        private bool? _any;

        private enum State
        {
            NoEnumerator,
            NoItem,
            FirstItem,
            AnyItem,
            End,
        }

        private State _state = State.NoEnumerator;

        /// <summary>
        /// Gets a value indicating whether the collection contains at least one element
        /// </summary>
        /// <value>
        ///   <c>true</c> if any; otherwise, <c>false</c>.
        /// </value>
        public override bool Any
        {
            get
            {
                if (!_any.HasValue)
                {
                    EnsureState(State.FirstItem);
                    _any = _enumeratedCount > 0;
                }
                return _any.Value;
            }
        }

        private bool? _hasFirst;
        private object _first;

        /// <summary>
        /// Gets the first element of collection.
        /// </summary>
        /// <value>
        /// The first.
        /// </value>
        /// <exception cref="System.InvalidOperationException"></exception>
        public override object First
        {
            get
            {
                if (!_hasFirst.HasValue)
                {
                    EnsureState(State.FirstItem);
                    _hasFirst = _enumeratedCount > 0;
                }
                if (_hasFirst.Value)
                    return _first;
                throw new InvalidOperationException();
            }
        }

        private object _current;

        private bool? _hasLast;
        private object _last;

        /// <summary>
        /// Gets the last element of collection
        /// </summary>
        /// <value>
        /// The last.
        /// </value>
        /// <exception cref="System.InvalidOperationException"></exception>
        public override object Last
        {
            get
            {
                if (!_hasLast.HasValue)
                {
                    EnsureState(State.End);
                    _hasLast = _enumeratedCount > 0;
                    _last = _current;
                }
                if (_hasLast.Value)
                    return _last;
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Enumerates all elements from collection
        /// </summary>
        /// <value>
        /// Elements
        /// </value>
        public override IEnumerable All => GetAll();

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableCollectionReader"/> class.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        public EnumerableCollectionReader(IEnumerable enumerable)
        {
            _enumerable = enumerable;
        }

        private void ResetEnumerator()
        {
            if (_enumerator == null)
                _enumerator = _enumerable.GetEnumerator();

            _state = State.NoItem;
            _enumeratedCount = 0;
            _enumerator.Reset();
        }

        private object EnumerateNext()
        {
            if (_state == State.End)
                return null;
            if (!_enumerator.MoveNext())
            {
                _state = State.End;
                return null;
            }

            _current = _enumerator.Current;

            if (_enumeratedCount++ == 0)
            {
                _state = State.FirstItem;
                _first = _current;
            }
            else
                _state = State.AnyItem;

            return _current;
        }

        /// <summary>
        /// Ensures we have reached a given state.
        /// </summary>
        /// <param name="state">The state.</param>
        private void EnsureState(State state)
        {
            if (_state >= state) return;
            ResetEnumerator();
            if (_state >= state) return;
            while (_state < state)
                EnumerateNext();
        }

        private IEnumerable<object> GetAll()
        {
            // because we handle if first item was already pulled
            EnsureState(State.FirstItem);

            // if after first (any or end), see what's in the collection
            if (_state > State.FirstItem)
            {
                // one total item, no need to restart, return the first and exit
                if (_enumeratedCount == 1)
                {
                    yield return _first;
                    yield break;
                }
                // otherwise, we have to restart
                ResetEnumerator();
            }

            // when collection is empty, end
            if (_enumeratedCount == 0)
                yield break;

            // first was extracted, we're just after it
            yield return _first;
            // so we loop
            while (_state < State.End)
                yield return EnumerateNext();
        }

        /// <summary>
        /// Gets the element at given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public override object At(int index)
        {
            EnsureState(State.NoItem);

            var enumerated = index + 1;
            if (_enumeratedCount > enumerated)
                ResetEnumerator();

            while (_enumeratedCount < enumerated)
            {
                // if enumeration ended before reaching index, it means the collection is too small
                if (_state == State.End)
                    throw new ArgumentOutOfRangeException();
                EnumerateNext();
            }
            return _current;
        }
    }
}