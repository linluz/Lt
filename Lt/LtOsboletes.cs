using Lt.Analysis;
using Lt.Majas;
using System.Drawing;

namespace Lt.Osbolete
{
    public class LTFT : AOComponent
    {
        public LTFT() :
            base("����������û����", "LTFT", "����", ID.LTMF_Osb, nameof(LTMF), LTResource.ɽ����û����)
        { }

        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "����", "Mt", "Ҫ����û��ɽ�ص�������");
            pm.AddIP(ParT.Number, "�߶�", "E", "��û���ε�ˮƽ��߶�");
            pm.AddIP(ParT.Colour, "ɫ��", "C", "��ˮ��û�����ɫ��", def: Color.FromArgb(52, 58, 107));

            pm.AddOP(ParT.Mesh, "����", "Mf", "��ˮ��û��ĵ�������");
        }
    }
    public class LTTG : AOComponent
    {
        public LTTG() : base("�¶ȷ���", "LTTG", "����", ID.LTMG_Osb, nameof(LTMG), LTResource.ɽ���¶ȷ���)
        { }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "����", "Mt", "Ҫ�����¶ȷ�����ɽ�ص�������");

            pm.AddOP(ParT.Mesh, "����", "M", "�Ѱ��Ƕ���ɫ�ĵ�������");
            pm.AddOP(ParT.Interval, "�Ƕ�", "A", "�¶ȷ�Χ���ȣ�");
        }
    }
    public class LTTE : AOComponent
    {
        public LTTE() : base("�̷߳���", "LTTE", "����", ID.LTME_Osb, nameof(LTME), LTResource.ɽ��̷߳���)
        { }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "����", "Mt", "Ҫ�����¶ȷ�����ɽ�ص�������");

            pm.AddOP(ParT.Mesh, "����", "M", "�Ѱ�������ɫ�ĵ�������");
            pm.AddOP(ParT.Interval, "����", "E", "���η�Χ����λС����");
        }
    }
    public class LTVL_Osb : AOComponent
    {
        public LTVL_Osb() : base("���߷���", "LTVL", "����", ID.LTVL_Osb, nameof(LTVL), LTResource.���߷���)
        { }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "����", "Mt", "Ҫ�����¶ȷ�����ɽ�ص�������,��֧�ֵ�������");
            pm.AddIP(ParT.Mesh, "�ϰ���", "O", "����ѡ���赲���ߵ��ϰ����壬,��֧�ֵ�������", ParamTrait.List | ParamTrait.Optional);
            pm.AddIP(ParT.Point, "�۲��", "P", "�۲������ڵĵ�λ�ã���һ���������ϣ���֧�ֶ��۲�,��֧�ֵ�������", ParamTrait.List);
            pm.AddIP(ParT.Integer, "����", "A", "�������ȣ������������ڵļ��,��֧�ֵ�������");

            pm.AddOP(ParT.Point, "�۲��", "O", "�۲����ӵ�λ�ã��۸�1m5��", ParamTrait.List);
            pm.AddOP(ParT.Point, "�ɼ���", "V", "�������ĵ�", ParamTrait.List);
        }
    }
    public class LTCE_Osb : AOComponent
    {
        public LTCE_Osb() : base("�ȸ��߸̷߳���", "LTCE", "����", ID.LTCE_Osb, nameof(LTCE), LTResource.�ȸ��߸̷߳���)
        { }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "�ȸ���", "C", "�������ĵȸ��ߣ�������ȷ������Ķ���ˮƽ����", ParamTrait.List);

            pm.AddOP(ParT.Colour, "ɫ��", "C", "�������߸̵߳�ӳ��ɫ��", ParamTrait.List);
            pm.AddOP(ParT.Interval, "��Χ", "R", "����߳��ߵĸ̷߳�Χ");
        }
    }
    public class LTCF_Osb : AOComponent
    {
        public LTCF_Osb() : base("�ȸ�����û����", "LTCE", "����",ID.LTCF_Osb, nameof(LTCF), LTResource.�ȸ�����û����)
        { }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "�ȸ���", "C", "Ҫ������û�����ĵȸ���", ParamTrait.List);
            pm.AddIP(ParT.Integer, "�߳�", "E", "ˮ��ĸ߳�");
            pm.AddIP(ParT.Boolean, "̯ƽ", "F", "�Ƿ�Ҫ��ˮ�µȸ���̯ƽ��ˮƽ�棬Ĭ��Ϊfalse", def: false);
            pm.AddIP(ParT.Colour, "ˮ��ɫ", "Cu", "δ��û���ȸ��ߵ�ɫ��", def: Color.White);
            pm.AddIP(ParT.Colour, "ˮ��ɫ", "Cd", "����û���ȸ��ߵ�ɫ��", def: Color.FromArgb(59, 104, 156));

            pm.AddOP(ParT.Curve, "ˮ����", "Cu", "δ��û���ĵȸ���");
            pm.AddOP(ParT.Curve, "ˮ����", "Cd", "����û���ĵȸ���");
        }
    }
}