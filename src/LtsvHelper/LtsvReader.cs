﻿using LtsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LtsvHelper
{
    /// <summary>
    /// Used to read LTSV files.
    /// </summary>
    public class LtsvReader : ILtsvReader
    {
        readonly LtsvParser _parser;

        readonly LtsvConfiguration _configuration;

        IDictionary<string, string> _currentRecord;

        bool _hasBeenRead = false;

        /// <summary>
        /// Initializes a new instance of <see cref="LtsvReader"/> class.
        /// </summary>
        /// <param name="textReader">The reader.</param>
        public LtsvReader(TextReader textReader)
            : this(textReader, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LtsvReader"/> class.
        /// </summary>
        /// <param name="textReader">The reader.</param>
        /// <param name="configuration">The configuration.</param>
        public LtsvReader(TextReader textReader, LtsvConfiguration configuration)
        {
            Ensure.ArgumentNotNull(textReader, nameof(textReader));

            _parser = new LtsvParser(textReader);
            _currentRecord = null;
            _configuration = configuration ?? new LtsvConfiguration();
        }

        /// <summary>
        /// Advances the reader to the next record.
        /// </summary>
        /// <returns>True if there are more records, otherwise false.</returns>
        public bool Read()
        {
            _currentRecord = _parser.Read();
            _hasBeenRead = true;
            if (_currentRecord != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Advances the reader to the next record.
        /// </summary>
        /// <returns>True if there are more records, otherwise false.</returns>
        public async Task<bool> ReadAsync()
        {
            _currentRecord = await _parser.ReadAsync().ConfigureAwait(false);
            _hasBeenRead = true;
            if (_currentRecord != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void CheckHasBeenRead()
        {
            if (!_hasBeenRead)
            {
                throw new LtsvReaderException("You must call read on the reader before accessing its data.");
            }
        }

        /// <summary>
        /// Gets the field at label.
        /// </summary>
        /// <param name="label">Tha label of the field.</param>
        /// <returns>The raw field.</returns>
        public string GetField(string label)
        {
            Ensure.ArgumentNotNullOrEmpty(label, nameof(label));

            CheckHasBeenRead();

            return _currentRecord[label];
        }

        /// <summary>
        /// Gets the field coverted to <typeparamref name="T"/> at label.
        /// </summary>
        /// <param name="label"></param>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <returns>The field coverted to <typeparamref name="T"/>.</returns>
        public T GetField<T>(string label)
        {
            Ensure.ArgumentNotNullOrEmpty(label, nameof(label));

            var field = GetField(label);
            return (T)Convert.ChangeType(field, typeof(T));
        }

        /// <summary>
        /// Gets the record coverted into <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the record.</typeparam>
        /// <returns>The record converted to <typeparamref name="T"/>.</returns>
        public T GetRecord<T>()
        {
            CheckHasBeenRead();

            var classMap = _configuration.GetClassMap(typeof(T));
            var record = (T)classMap.Constructor();
            foreach (var p in classMap.PropertyMaps)
            {
                if (_currentRecord.ContainsKey(p.LabelString))
                {
                    var field = GetField(p.LabelString);
                    var value = p.TypeConverter.ConvertFromString(field, this, p);
                    p.Setter(record, value);
                }
            }
            return record;
        }

        /// <summary>
        /// Gets all the records in the LTSV file and converts each to
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the record.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> of records.</returns>
        public IEnumerable<T> GetRecords<T>()
        {
            while (Read())
            {
                yield return GetRecord<T>();
            }
        }

        #region IDisposable Support
        private bool _disposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing,
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if the instance needs to be disposed of.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _parser.Dispose();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing,
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
