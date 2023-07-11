using System;
using System.Collections.Generic;
using UnityEngine;

namespace XCharts.Runtime
{
    public enum AnimationType
    {
        /// <summary>
        /// he default. An animation playback mode will be selected according to the actual situation.
        /// |默认。内部会根据实际情况选择一种动画播放方式。
        /// </summary>
        Default,
        /// <summary>
        /// Play the animation from left to right.
        /// |从左往右播放动画。
        /// </summary>
        LeftToRight,
        /// <summary>
        /// Play the animation from bottom to top.
        /// |从下往上播放动画。
        /// </summary>
        BottomToTop,
        /// <summary>
        /// Play animations from the inside out.
        /// |由内到外播放动画。
        /// </summary>
        InsideOut,
        /// <summary>
        /// Play the animation along the path.
        /// |沿着路径播放动画。当折线图从左到右无序或有折返时，可以使用该模式。
        /// </summary>
        AlongPath,
        /// <summary>
        /// Play the animation clockwise.
        /// |顺时针播放动画。
        /// </summary>
        Clockwise,
    }

    public enum AnimationEasing
    {
        Linear,
    }

    /// <summary>
    /// the animation of serie. support animation type: fadeIn, fadeOut, change, addition.
    /// |动画表现。支持配置四种动画表现：FadeIn（渐入动画），FadeOut（渐出动画），Change（变更动画），Addition（新增动画）。
    /// </summary>
    [System.Serializable]
    public class AnimationStyle : ChildComponent
    {
        [SerializeField] private bool m_Enable = true;
        [SerializeField] private AnimationType m_Type;
        [SerializeField] private AnimationEasing m_Easting;
        [SerializeField] private int m_Threshold = 2000;
        [SerializeField][Since("v3.4.0")] private bool m_UnscaledTime;
        [SerializeField][Since("v3.8.0")] private AnimationFadeIn m_Fadein = new AnimationFadeIn();
        [SerializeField][Since("v3.8.0")] private AnimationFadeOut m_Fadeout = new AnimationFadeOut() { reverse = true };
        [SerializeField][Since("v3.8.0")] private AnimationChange m_Change = new AnimationChange() { duration = 500 };
        [SerializeField][Since("v3.8.0")] private AnimationAddition m_Addition = new AnimationAddition() { duration = 500 };

        [Obsolete("Use animation.fadeIn.delayFunction instead.", true)]
        public AnimationDelayFunction fadeInDelayFunction;
        [Obsolete("Use animation.fadeIn.durationFunction instead.", true)]
        public AnimationDurationFunction fadeInDurationFunction;
        [Obsolete("Use animation.fadeOut.delayFunction instead.", true)]
        public AnimationDelayFunction fadeOutDelayFunction;
        [Obsolete("Use animation.fadeOut.durationFunction instead.", true)]
        public AnimationDurationFunction fadeOutDurationFunction;
        [Obsolete("Use animation.fadeIn.OnAnimationEnd() instead.", true)]
        public Action fadeInFinishCallback { get; set; }
        [Obsolete("Use animation.fadeOut.OnAnimationEnd() instead.", true)]
        public Action fadeOutFinishCallback { get; set; }
        public AnimationStyleContext context = new AnimationStyleContext();

        /// <summary>
        /// Whether to enable animation.
        /// |是否开启动画效果。
        /// </summary>
        public bool enable { get { return m_Enable; } set { m_Enable = value; } }
        /// <summary>
        /// The type of animation.
        /// |动画类型。
        /// </summary>
        public AnimationType type { get { return m_Type; } set { m_Type = value; } }
        /// <summary>
        /// Whether to set graphic number threshold to animation. Animation will be disabled when graphic number is larger than threshold.
        /// |是否开启动画的阈值，当单个系列显示的图形数量大于这个阈值时会关闭动画。
        /// </summary>
        public int threshold { get { return m_Threshold; } set { m_Threshold = value; } }
        /// <summary>
        /// Animation updates independently of Time.timeScale.
        /// |动画是否受TimeScaled的影响。默认为 false 受TimeScaled的影响。
        /// </summary>
        public bool unscaledTime { get { return m_UnscaledTime; } set { m_UnscaledTime = value; } }
        /// <summary>
        /// Fade in animation configuration.
        /// |渐入动画配置。
        /// </summary>
        public AnimationFadeIn fadein { get { return m_Fadein; } }
        /// <summary>
        /// Fade out animation configuration.
        /// |渐出动画配置。
        /// </summary>
        public AnimationFadeOut fadeout { get { return m_Fadeout; } }
        /// <summary>
        /// Update data animation configuration.
        /// |数据变更动画配置。
        /// </summary>
        public AnimationChange change { get { return m_Change; } }
        /// <summary>
        /// Add data animation configuration.
        /// |数据新增动画配置。
        /// </summary>
        public AnimationAddition addition { get { return m_Addition; } }

        private Vector3 m_LinePathLastPos;
        private List<AnimationInfo> m_Animations;
        private List<AnimationInfo> animations
        {
            get
            {
                if (m_Animations == null)
                {
                    m_Animations = new List<AnimationInfo>();
                    m_Animations.Add(m_Fadein);
                    m_Animations.Add(m_Fadeout);
                    m_Animations.Add(m_Change);
                    m_Animations.Add(m_Addition);
                }
                return m_Animations;
            }
        }

        public AnimationInfo activedAnimation
        {
            get
            {
                foreach (var anim in animations)
                {
                    if (anim.context.start) return anim;
                }
                return null;
            }
        }

        public void Fadein()
        {
            if (m_Fadeout.context.start) return;
            m_Fadein.Start();
        }

        public void Restart()
        {
            var anim = activedAnimation;
            Reset();
            if (anim != null)
            {
                anim.Start();
            }
        }

        public void Fadeout()
        {
            m_Fadeout.Start();
        }

        public void Addition()
        {
            if (!enable) return;
            if (!m_Fadein.context.start && !m_Fadeout.context.start)
            {
                m_Addition.Start(false);
            }
        }

        public void Pause()
        {
            foreach (var anim in animations)
            {
                anim.Pause();
            }
        }

        public void Resume()
        {
            foreach (var anim in animations)
            {
                anim.Resume();
            }
        }

        public void Reset()
        {
            m_Fadein.Reset();
            m_Fadeout.Reset();
        }

        public void InitProgress(float curr, float dest)
        {
            var anim = activedAnimation;
            if (anim == null) return;
            var isAddedAnim = anim is AnimationAddition;
            if (IsIndexAnimation())
            {
                if (isAddedAnim)
                {
                    anim.Init(anim.context.currPointIndex, dest, (int)dest - 1);
                }
                else
                {
                    m_Addition.context.currPointIndex = (int)dest - 1;
                    anim.Init(curr, dest, (int)dest - 1);
                }
            }
            else
            {
                anim.Init(curr, dest, 0);
            }
        }

        public void InitProgress(List<Vector3> paths, bool isY)
        {
            if (paths.Count < 1) return;
            var anim = activedAnimation;
            if (anim == null) return;
            var isAddedAnim = anim is AnimationAddition;
            var startIndex = 0;
            if (isAddedAnim)
            {
                startIndex = anim.context.currPointIndex == paths.Count - 1 ?
                    paths.Count - 2 :
                    anim.context.currPointIndex;
            }
            else
            {
                m_Addition.context.currPointIndex = paths.Count - 1;
            }
            var sp = paths[startIndex];
            var ep = paths[paths.Count - 1];
            var currDetailProgress = isY ? sp.y : sp.x;
            var totalDetailProgress = isY ? ep.y : ep.x;
            if (context.type == AnimationType.AlongPath)
            {
                currDetailProgress = 0;
                totalDetailProgress = 0;
                var lp = sp;
                for (int i = 1; i < paths.Count; i++)
                {
                    var np = paths[i];
                    totalDetailProgress += Vector3.Distance(np, lp);
                    lp = np;
                    if (startIndex > 0 && i == startIndex)
                        currDetailProgress = totalDetailProgress;
                }
                m_LinePathLastPos = sp;
                context.currentPathDistance = 0;
            }
            anim.Init(currDetailProgress, totalDetailProgress, paths.Count - 1);
        }

        public bool IsEnd()
        {
            foreach (var animation in animations)
            {
                if (animation.context.start)
                    return animation.context.end;
            }
            return m_Fadein.context.end;
        }


        public bool IsFinish()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return true;
#endif
            if (!m_Enable)
                return true;
            foreach (var animation in animations)
            {
                if (animation.context.start && animation.context.end)
                {
                    return true;
                }
            }
            if (IsIndexAnimation())
            {
                if (m_Fadeout.context.start) return m_Fadeout.context.currProgress <= m_Fadeout.context.destProgress;
                else if (m_Addition.context.start) return m_Addition.context.currProgress >= m_Addition.context.destProgress;
                else return m_Fadein.context.currProgress >= m_Fadein.context.destProgress;
            }
            else if (IsItemAnimation())
            {
                return false;
            }
            return true;
        }

        public bool IsInDelay()
        {
            var anim = activedAnimation;
            if (anim != null)
                return anim.IsInDelay();
            return false;
        }

        public bool IsItemAnimation()
        {
            return context.type == AnimationType.BottomToTop || context.type == AnimationType.InsideOut;
        }

        public bool IsIndexAnimation()
        {
            return context.type == AnimationType.LeftToRight ||
                context.type == AnimationType.AlongPath || context.type == AnimationType.Clockwise;
        }

        public bool CheckDetailBreak(float detail)
        {
            if (!IsIndexAnimation())
                return false;
            foreach (var animation in animations)
            {
                if (animation.context.start)
                    return !IsFinish() && detail > animation.context.currProgress;
            }
            return false;
        }

        public bool CheckDetailBreak(Vector3 pos, bool isYAxis)
        {
            if (!IsIndexAnimation())
                return false;

            if (IsFinish())
                return false;

            if (context.type == AnimationType.AlongPath)
            {
                context.currentPathDistance += Vector3.Distance(pos, m_LinePathLastPos);
                m_LinePathLastPos = pos;
                return CheckDetailBreak(context.currentPathDistance);
            }
            else
            {
                if (isYAxis)
                    return pos.y > GetCurrDetail();
                else
                    return pos.x > GetCurrDetail();
            }
        }

        public void CheckProgress()
        {
            if (IsItemAnimation() && context.isAllItemAnimationEnd)
            {
                foreach (var animation in animations)
                {
                    animation.End();
                }
                return;
            }
            foreach (var animation in animations)
            {
                animation.CheckProgress(animation.context.totalProgress, m_UnscaledTime);
            }
        }

        public void CheckProgress(double total)
        {
            if (IsFinish())
                return;
            foreach (var animation in animations)
            {
                animation.CheckProgress(total, m_UnscaledTime);
            }
        }

        internal float CheckItemProgress(int dataIndex, float destProgress, ref bool isEnd, float startProgress = 0)
        {
            isEnd = false;
            var anim = activedAnimation;
            if (anim == null) return destProgress;
            return anim.CheckItemProgress(dataIndex, destProgress, ref isEnd, startProgress, m_UnscaledTime);
        }

        public void CheckSymbol(float dest)
        {
            m_Fadein.CheckSymbol(dest, m_UnscaledTime);
            m_Fadeout.CheckSymbol(dest, m_UnscaledTime);
        }

        public float GetSysmbolSize(float dest)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return dest;
#endif
            if (!enable)
                return dest;

            if (IsEnd())
                return m_Fadeout.context.start ? 0 : dest;

            return m_Fadeout.context.start ? m_Fadeout.context.sizeProgress : m_Fadein.context.sizeProgress;
        }

        public float GetCurrDetail()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                foreach (var animation in animations)
                {
                    if (animation.context.start)
                        return animation.context.destProgress;
                }
            }
#endif
            foreach (var animation in animations)
            {
                if (animation.context.start)
                    return animation.context.currProgress;
            }
            return m_Fadein.context.currProgress;
        }

        public float GetCurrRate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return 1;
#endif
            if (!enable || IsEnd())
                return 1;
            return m_Fadeout.context.start ? m_Fadeout.context.currProgress : m_Fadein.context.currProgress;
        }

        public int GetCurrIndex()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return -1;
#endif
            if (!enable)
                return -1;
            var anim = activedAnimation;
            if (anim == null)
                return -1;
            return (int)anim.context.currProgress;
        }

        public float GetChangeDuration()
        {
            if (m_Enable && m_Change.enable)
                return m_Change.duration;
            else
                return 0;
        }

        public float GetAdditionDuration()
        {
            if (m_Enable && m_Addition.enable)
                return m_Addition.duration;
            else
                return 0;
        }

        public bool HasFadeout()
        {
            return enable && m_Fadeout.context.end;
        }
    }
}