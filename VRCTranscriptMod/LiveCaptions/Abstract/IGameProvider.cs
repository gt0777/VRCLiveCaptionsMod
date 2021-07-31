using System;
using UnityEngine;

namespace VRCLiveCaptionsMod.LiveCaptions.Abstract {
    /// <summary>
    /// An audio source that emits audio
    /// </summary>
    public interface IAudioSource {
        /// <summary>
        /// Gets the position of the audio source in world-space
        /// </summary>
        /// <returns>The position in world-space</returns>
        Vector3 GetPosition();

        /// <summary>
        /// Get a unique ID of the audio source. This may be used with TranscriptPlayerOverrides
        /// </summary>
        /// <returns>The unique ID</returns>
        string GetUID();

        /// <summary>
        /// Gets a user-readable friendly name of the audio source.
        /// This name may not neccessarily be unique.
        /// </summary>
        /// <returns>The user-readable friendly display name</returns>
        string GetFriendlyName();
    };

    /// <summary>
    /// Provides necessary utilities that are needed for adding
    /// transcriptions to a game
    /// </summary>
    public interface IGameProvider {
        /// <summary>
        /// Event when audio is emitted.
        /// <param name="IAudioSource">The audio source that has emitted audio</param>
        /// <param name="float_array">The raw samples that have been emitted. The values should be between -1.0 and 1.0</param>
        /// <param name="int0">The number of samples that have been emitted.
        /// It's possible for float_array to be longer than this, but only this number of samples
        /// should be extracted from float_array.</param>
        /// <param name="int1">The sample rate of the audio</param>
        /// </summary>
        event Action<IAudioSource, float[], int, int> AudioEmitted;

        /// <summary>
        /// Fired when an audio source has been added to the game.
        /// </summary>
        event Action<IAudioSource> AudioSourceAdded;

        /// <summary>
        /// Fired when an audio source has been removed from the game.
        /// </summary>
        event Action<IAudioSource> AudioSourceRemoved;

        /// <summary>
        /// Fired when ALL audio sources are being removed (for example, the player is leaving the room in VRChat)
        /// Transcription should stop immediately upon this happening.
        /// </summary>
        event Action AllAudioSourcesRemoved;


        /// <summary>
        /// Gets the local head position in world-space.
        /// </summary>
        /// <returns>The local head position in world-space</returns>
        Vector3 GetLocalHeadPosition();
    }
}
