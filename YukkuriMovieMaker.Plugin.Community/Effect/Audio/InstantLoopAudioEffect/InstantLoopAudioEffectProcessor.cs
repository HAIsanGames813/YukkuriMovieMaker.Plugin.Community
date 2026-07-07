using YukkuriMovieMaker.Player.Audio.Effects;

namespace YukkuriMovieMaker.Plugin.Community.Effect.Audio.InstantLoopAudioEffect
{
    internal class InstantLoopAudioEffectProcessor(InstantLoopAudioEffect item) : AudioEffectProcessorBase
    {
        const int Channels = 2;

        readonly InstantLoopAudioEffect _item = item;

        public override int Hz => Input?.Hz ?? 0;
        public override long Duration => Input?.Duration ?? 0;

        protected override void seek(long outputPos)
        {
            if (Input is null) return;

            var info = Resolve(outputPos / Channels);
            if (!info.IsGap)
                Input.Seek(info.SourceFrame * Channels);
        }
        protected override int read(float[] buffer, int offset, int count)
        {
            if (Input is null) return 0;

            int written = 0;

            while (written < count)
            {
                int remaining = count - written;
                long currentFrame = (Position + written) / Channels;
                long totalFrames = Duration / Channels;
                long startOffsetFrames = MsToFrames(
                    _item.StartOffset.GetValue(currentFrame, totalFrames, Hz));
                long intervalFrames = Math.Max(1L, MsToFrames(
                    _item.Interval.GetValue(currentFrame, totalFrames, Hz)));
                long gapFrames = Math.Max(0L, MsToFrames(
                    _item.Gap.GetValue(currentFrame, totalFrames, Hz)));
                int repeatCountInt = (int)Math.Round(
                    _item.RepeatCount.GetValue(currentFrame, totalFrames, Hz));
                bool infinite = repeatCountInt <= 0;

                long cycleFrames = intervalFrames + gapFrames;
                long loopEndFrame = infinite ? long.MaxValue : (long)repeatCountInt * cycleFrames;
                long sourceFrame;
                bool isGap;
                long framesUntilBoundary;

                if (infinite || currentFrame < loopEndFrame)
                {
                    long posInCycle = currentFrame % cycleFrames;

                    if (posInCycle < intervalFrames)
                    {                        
                        isGap = false;
                        sourceFrame = startOffsetFrames + posInCycle;
                        long toEndOfPlay = intervalFrames - posInCycle;
                        framesUntilBoundary = infinite
                            ? toEndOfPlay
                            : Math.Min(toEndOfPlay, loopEndFrame - currentFrame);
                    }
                    else
                    {
                        isGap = true;
                        sourceFrame = -1;
                        long toEndOfGap = cycleFrames - posInCycle;
                        framesUntilBoundary = infinite
                            ? toEndOfGap
                            : Math.Min(toEndOfGap, loopEndFrame - currentFrame);
                    }
                }
                else
                {
                    isGap = false;
                    long pastLoopFrames = currentFrame - loopEndFrame;
                    sourceFrame = startOffsetFrames + intervalFrames + pastLoopFrames;
                    framesUntilBoundary = long.MaxValue;
                }
                long maxFrames = framesUntilBoundary == long.MaxValue
                    ? (long)(remaining / Channels)
                    : Math.Min(framesUntilBoundary, (long)(remaining / Channels));
                int chunkFloats = (int)(maxFrames * Channels);

                if (chunkFloats <= 0)
                {
                    if (remaining > 0)
                    {
                        Array.Clear(buffer, offset + written, remaining);
                        written += remaining;
                    }
                    break;
                }

                if (isGap)
                {
                    Array.Clear(buffer, offset + written, chunkFloats);
                    written += chunkFloats;
                }
                else
                {
                    long expectedInputPos = sourceFrame * Channels;
                    if (Input.Position != expectedInputPos)
                        Input.Seek(expectedInputPos);

                    int readCount = Input.Read(buffer, offset + written, chunkFloats);
                    if (readCount <= 0)
                    {
                        //ソース終端に達した場合もこのチャンク分だけ無音化して続行する
                        //（次のサイクルで巻き戻せばまだ音声が存在する可能性がある）
                        Array.Clear(buffer, offset + written, chunkFloats);
                        written += chunkFloats;
                        continue;
                    }
                    written += readCount;
                }
            }

            return written;
        }
        readonly record struct FrameInfo(long SourceFrame, bool IsGap);

        FrameInfo Resolve(long currentFrame)
        {
            long totalFrames = Duration / Channels;
            long startOffsetFrames = MsToFrames(_item.StartOffset.GetValue(currentFrame, totalFrames, Hz));
            long intervalFrames = Math.Max(1L, MsToFrames(_item.Interval.GetValue(currentFrame, totalFrames, Hz)));
            long gapFrames = Math.Max(0L, MsToFrames(_item.Gap.GetValue(currentFrame, totalFrames, Hz)));
            int repeatCountInt = (int)Math.Round(_item.RepeatCount.GetValue(currentFrame, totalFrames, Hz));
            bool infinite = repeatCountInt <= 0;
            long cycleFrames = intervalFrames + gapFrames;
            long loopEndFrame = infinite ? long.MaxValue : (long)repeatCountInt * cycleFrames;

            if (infinite || currentFrame < loopEndFrame)
            {
                long posInCycle = currentFrame % cycleFrames;
                if (posInCycle < intervalFrames)
                    return new FrameInfo(startOffsetFrames + posInCycle, false);
                else
                    return new FrameInfo(-1, true);
            }
            else
            {
                long pastLoopFrames = currentFrame - loopEndFrame;
                return new FrameInfo(startOffsetFrames + intervalFrames + pastLoopFrames, false);
            }
        }

        long MsToFrames(double ms) => (long)(ms / 1000.0 * Hz);
    }
}