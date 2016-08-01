﻿using System;

namespace Axinom.Cpix
{
	public sealed class ContentKey : IContentKey
	{
		/// <summary>
		/// Unique ID of the content key.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the value of the content key. Must be 128 bits long.
		/// </summary>
		public byte[] Value { get; set; }

		/// <summary>
		/// Vertfies that the class represents a valid newly created content key.
		/// </summary>
		internal void ValidateNewContentKey()
		{
			if (Id == Guid.Empty)
				throw new InvalidCpixDataException("A unique key ID must be provided for each content key.");

			if (Value == null || Value.Length != Constants.ContentKeyLengthInBytes)
				throw new InvalidCpixDataException($"A {Constants.ContentKeyLengthInBytes}-byte value must be provided for each new content key.");
		}
	}
}
