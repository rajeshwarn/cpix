﻿using Axinom.Cpix;
using System;
using Xunit;

namespace Tests
{
	public sealed class ResolveContentKeyTests
	{
		[Fact]
		public void ResolveContentKey_WithNoRules_ThrowsException()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(key);

			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription()));
		}

		[Fact]
		public void ResolveContentKey_WithMultipleMatches_ThrowsException()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(key);

			// Two rules that match all samples - resolving a key will never work with this document.
			document.UsageRules.Add(new UsageRule
			{
				KeyId = key.Id
			});
			document.UsageRules.Add(new UsageRule
			{
				KeyId = key.Id
			});

			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription()));
		}

		[Fact]
		public void ResolveContentKEy_WithRulesButNoMatch_ThrowsException()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(key);

			document.UsageRules.Add(new UsageRule
			{
				KeyId = key.Id,
				AudioFilters = new[]
				{
					new AudioFilter()
				}
			});

			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Video
			}));
		}

		[Fact]
		public void ResolveContentKey_WithSingleMatch_Succeeds()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(key);

			document.UsageRules.Add(new UsageRule
			{
				KeyId = key.Id,
				AudioFilters = new[]
				{
					new AudioFilter()
				}
			});

			Assert.Equal(key, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio
			}));
		}
		
		[Fact]
		public void ResolveContentKey_WithRealisticFilters_MatchesAsExpected()
		{
			var lowValuePeriod1 = TestHelpers.GenerateContentKey();
			var mediumValuePeriod1 = TestHelpers.GenerateContentKey();
			var highValuePeriod1 = TestHelpers.GenerateContentKey();
			var lowValueAudio = TestHelpers.GenerateContentKey();
			var highValueAudio = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(lowValuePeriod1);
			document.ContentKeys.Add(mediumValuePeriod1);
			document.ContentKeys.Add(highValuePeriod1);
			document.ContentKeys.Add(lowValueAudio);
			document.ContentKeys.Add(highValueAudio);

			const int mediumValuePixelCount = 1280 * 720;
			const int highValuePixelCount = 1920 * 1080;

			document.UsageRules.Add(new UsageRule
			{
				KeyId = lowValuePeriod1.Id,
				VideoFilters = new[]
				{
					new VideoFilter
					{
						MaxPixels = mediumValuePixelCount - 1
					}
				}
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = mediumValuePeriod1.Id,
				VideoFilters = new[]
				{
					new VideoFilter
					{
						MinPixels = mediumValuePixelCount,
						MaxPixels = highValuePixelCount - 1
					}
				}
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = highValuePeriod1.Id,
				VideoFilters = new[]
				{
					new VideoFilter
					{
						MinPixels = highValuePixelCount
					}
				}
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = lowValueAudio.Id,
				AudioFilters = new[]
				{
					new AudioFilter
					{
						MaxChannels = 2
					}
				}
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = highValueAudio.Id,
				AudioFilters = new[]
				{
					new AudioFilter
					{
						MinChannels = 3
					}
				}
			});

			// Audio with unknown channel count should not match either rule.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio
			}));

			// If we do have a channel count, however, it should match the appropriate rule.
			Assert.Equal(lowValueAudio, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				AudioChannelCount = 1
			}));

			// Audio rules have no time range, so it should match even outside the defined periods.
			Assert.Equal(lowValueAudio, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				AudioChannelCount = 1
			}));

			// Or, indeed, even without any known timestamp.
			Assert.Equal(lowValueAudio, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				AudioChannelCount = 1
			}));

			// There is a rollover to a new key at 3 channels.
			Assert.Equal(lowValueAudio, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				AudioChannelCount = 2
			}));
			Assert.Equal(highValueAudio, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				AudioChannelCount = 3
			}));

			// Video samples without resolution should not match any rule.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Video
			}));

			// Extra data should not change anything in matching.
			Assert.Equal(highValuePeriod1, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Video,
				PicturePixelCount = highValuePixelCount * 2,
				Bitrate = 254726,
			}));
		}

		[Fact]
		public void ResolveContentKey_WithLabeledSamples_MatchesWithAnyMatchingLabel()
		{
			var key1 = TestHelpers.GenerateContentKey();
			var key2 = TestHelpers.GenerateContentKey();
			var key3 = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(key1);
			document.ContentKeys.Add(key2);
			document.ContentKeys.Add(key3);

			document.UsageRules.Add(new UsageRule
			{
				KeyId = key1.Id,
				AudioFilters = new[]
				{
					new AudioFilter(),
				},
				LabelFilters = new[]
				{
					new LabelFilter
					{
						Label = "label1"
					}
				}
			});
			document.UsageRules.Add(new UsageRule
			{
				KeyId = key2.Id,
				VideoFilters = new[]
				{
					new VideoFilter(),
				},
				LabelFilters = new[]
				{
					new LabelFilter
					{
						Label = "label2"
					}
				}
			});
			document.UsageRules.Add(new UsageRule
			{
				KeyId = key3.Id,
				LabelFilters = new[]
				{
					new LabelFilter
					{
						Label = "label3"
					}
				}
			});

			Assert.Equal(key1, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				Labels = new[]
				{
					"label1"
				}
			}));

			Assert.Equal(key1, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				Labels = new[]
				{
					"label1"
				}
			}));

			// Unrelated labels should not be an obstacle to matching.
			Assert.Equal(key1, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				Labels = new[]
				{
					"label1",
					"xxxyyyzz"
				}
			}));

			Assert.Equal(key2, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Video,
				Labels = new[]
				{
					"label1",
					"label2",
					"someVideoLabel"
				}
			}));

			// Labels from otherwise nonmatching rules should have no effects.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Video,
				Labels = new[]
				{
					"label1",
				}
			}));

			// Multiple matches are evil and should not be accepted.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Video,
				Labels = new[]
				{
					"label2",
					"label3",
				}
			}));

			// A match is a match regardless of other parameters.
			Assert.Equal(key3, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				Labels = new[]
				{
					"label3"
				},
				AudioChannelCount = 3,
				Bitrate = 63576
			}));

			Assert.Equal(key3, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Video,
				Labels = new[]
				{
					"label3"
				},
				Bitrate = 123,
				PicturePixelCount = 46733,
			}));
		}
	}
}
