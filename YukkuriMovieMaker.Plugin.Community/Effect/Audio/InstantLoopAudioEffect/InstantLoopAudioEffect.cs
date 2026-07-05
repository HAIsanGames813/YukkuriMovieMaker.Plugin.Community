using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Audio.Effects;
using YukkuriMovieMaker.Plugin.Effects;

namespace YukkuriMovieMaker.Plugin.Community.Effect.Audio.InstantLoopAudioEffect
{
    [AudioEffect(nameof(Texts.plugin_name),[AudioEffectCategories.Effect],["loop", "stutter", "instant loop", nameof(Texts.tag_loop), nameof(Texts.tag_stutter)],IsAviUtlSupported = false,ResourceType = typeof(Texts))]
    internal class InstantLoopAudioEffect : AudioEffectBase
    {
        public override string Label => Texts.plugin_name;

        [Display(GroupName = nameof(Texts.group_name), Name = nameof(Texts.param_start_offset_name), Description = nameof(Texts.param_start_offset_desc), ResourceType = typeof(Texts))]
        [AnimationSlider("F0", "ms", 0, 10000)]
        public Animation StartOffset { get; } = new Animation(0, 0, 100000);

        [Display(GroupName = nameof(Texts.group_name), Name = nameof(Texts.param_interval_name), Description = nameof(Texts.param_interval_desc), ResourceType = typeof(Texts))]
        [AnimationSlider("F0", "ms", 1, 10000)]
        public Animation Interval { get; } = new Animation(500, 1, 100000);

        [Display(GroupName = nameof(Texts.group_name), Name = nameof(Texts.param_gap_name), Description = nameof(Texts.param_gap_desc), ResourceType = typeof(Texts))]
        [AnimationSlider("F0","ms", 0, 10000)]
        public Animation Gap { get; } = new Animation(0, 0, 100000);

        [Display(GroupName = nameof(Texts.group_name), Name = nameof(Texts.param_repeat_count_name), Description = nameof(Texts.param_repeat_count_desc), ResourceType = typeof(Texts))]
        [AnimationSlider("F0",nameof(Texts.unit_count), 0, 100, ResourceType = typeof(Texts))]
        public Animation RepeatCount { get; } = new Animation(0, 0, 10000);

        public override IAudioEffectProcessor CreateAudioEffect(TimeSpan duration)
            => new InstantLoopAudioEffectProcessor(this);

        public override IEnumerable<string> CreateExoAudioFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
            => [];

        protected override IEnumerable<IAnimatable> GetAnimatables()
            => [StartOffset, Interval, Gap, RepeatCount];
    }
}