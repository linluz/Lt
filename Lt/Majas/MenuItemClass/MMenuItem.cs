using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;

namespace Lt.Majas.MenuItemClass
{
    /// <summary>
    /// 菜单项基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MMenuItem<T>
    {
        protected MMenuItem(MComponent c, string text, T def, bool recom = false, bool rw = true)
        {
            Name = text;
            NameRW = "右键" + Name;
            var t0 = Name.IndexOf("(&", StringComparison.Ordinal);
            NameNoKey = t0 > 0 ? Name.Substring(0, t0) : Name;

            Component = c;
            Def = def;
            ReCom = recom;
            if (rw)
            {
                Component.WriteL.Add(WriteBase);
                Component.ReadL.Add(ReadBase);
            }
            ReadL = new List<Func<GH_IReader, bool>>(5);
            WriteL = new List<Func<GH_IWriter, bool>>(5);
        }

        #region RW
        private bool ReadBase(GH_IReader r)
        {
            GH_IReader c = r.FindChunk(NameRW);
            if (c != null) return ReadL.Any(t => t.Invoke(c));
            Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{Component.NickName}电池找不到右键项{NameRW}的写入块，\r\n可能是插件更新，请不要保存并联系作者火速修复，\r\n 可能是文档已损坏，请自行修复");
            return false;
        }
        private bool WriteBase(GH_IWriter w)
        {
            if (w.Chunks.Any(t => t.Name == NameRW))
            {
                Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{Component.NickName}电池找到多个重名写入块{NameRW},这会导致读写混乱，请联系作者修复");
                return false;
            }
            GH_IWriter c = w.CreateChunk(NameRW);
            return WriteL.Any(t => t.Invoke(c));
        }

        internal readonly List<Func<GH_IReader, bool>> ReadL;
        internal readonly List<Func<GH_IWriter, bool>> WriteL;

        protected bool ItemExist(GH_IReader r, string name)
        {
            if (r.ItemExists(name)) return true;
            Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{Component.NickName}电池的{Name}找不到{name}项，请联系开发者修复");
            return false;

        }
        protected bool ItemNoExist(GH_IWriter w, string name)
        {
            if (w.Items.All(t => t.Name != name)) return true;
            Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{Component.NickName}电池的{Name}找到多个{name}项，请联系开发者修复");
            return false;
        }
        protected bool ChunkNoExist(GH_IWriter w, string name)
        {
            if (w.Chunks.All(t => t.Name != name)) return true;
            Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{Component.NickName}电池的{Name}找到多个{name}块，请联系开发者修复");
            return false;
        }
        protected bool ChunkExist(GH_IReader r, string name)
        {
            if (r.ChunkExists(name)) return true;
            Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{Component.NickName}电池的{Name}找不到{name}块，请联系开发者修复");
            return false;
        }
        #endregion

        #region Message
        /// <summary>
        /// 设置信息函数，若输入为空则不会被设置
        /// </summary>
        /// <param name="f">信息更新函数</param>
        protected void SetMessage<Q>(Func<Q, string> f) where Q : MMenuItem<T>
        {
            if (f == null) return;
            MessageF = m => f.Invoke((Q)m);
            Component.MessageFl.Add(ToMessage);
            Component.ToMessage();//初始化的时候显示信息
        }

        /// <summary>
        /// 输出信息字串
        /// </summary>
        /// <returns>输出的信息字串</returns>
        protected string ToMessage()
            => MessageF == null ? "" : MessageF.Invoke(this);

        protected Func<MMenuItem<T>, string> MessageF;
        #endregion

        public readonly string Name;
        public T Def
        {
            get => _def;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(Def), "菜单项的默认值不能赋null值");
                if (_def != null && _def.GetHashCode() == value.GetHashCode()) return;
                //值改变才赋值
                _def = value;
                if (MessageF != null)//有信息才更新信息
                    Component.ToMessage();
            }
        }

        private T _def;
        public bool ReCom;
        public ToolStripMenuItem Item;
        protected MComponent Component;
        protected bool IsVaild0 => Item != null && Component != null;
        private readonly string NameRW;
        public string NameNoKey;
    }
}
