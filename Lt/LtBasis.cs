using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Lt.Majas;
using Grasshopper.GUI.Gradient;
using Rhino.Display;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Parameters;


namespace Lt.Basis
{

    /// <summary>
    /// ����
    /// Revcloud
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class RCloud : AComponent
    {
        public RCloud() : base(
            "����", "RCloud",
            "ʰȡ�����߿��Զ���������,��һ��ʰȡ����߿�",
             "����",
            ID.RCloud, 1, LTResource.����) { }
        //AddIntegerParameter �� item ���ɱ���

        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Curve, "�����߿�", "B", "���ߵĻ����߿�,ÿ�����߱���ƽ��պ�");
            pm.AddIP(ParT.Number, "��С����", "Amin", "����Բ������С����,����С��0.5���Ҳ��ܴ����߿��1/3");
            pm.AddIP(ParT.Number, "��󻡳�", "Amax", "(��ѡ)����Բ������󳤶ȣ���������Ϊ�̶�����", ParamTrait.Item | ParamTrait.Optional);
            pm.AddIP(ParT.Boolean, "��ת����", "R", "��ת���ߵ����⳯��Ĭ������\r\ntrueΪ���⣬falseΪ����", def: true);
            pm.AddIP(ParT.Integer, "�������", "S", "Բ������ֲ������Ĭ��Ϊ255������󻡳�������ʱ��Ч", def: 225);

            pm.AddOP(ParT.Curve, "����", "C", "���ɵ�����");
            pm.AddOP(ParT.Interval, "��������", "I", "ʵ�����ɵĻ�����Χ����");
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region ��ʼ�� ��ȡ����
            Curve b = new PolyCurve();
            double max = 0;
            double min = 0;
            bool r = false;
            int s = 0;
            if (!DA.GetData(0, ref b) ||
                RMNoValid(b, 0) || RMNoClosed(b, 0) || RNNoPlanar(b, 0) || //��ȡ�߿򣬲����պ���ƽ��
                !DA.GetData(1, ref min) || RMSmaller(min, 0.5, 1, equal: true) || //��ȡ��Сֵ������Ƿ����0.5
                RMLarger(min * 3, b.GetLength(), 1, 0, tb: NumT.Length) || //���3����Сֵ�Ƿ�����߳�
                !DA.GetData(3, ref r))
                return;
            var bmax = DA.GetData(2, ref max);
            if (bmax && RMSmaller(max, min, 2, 1) //�ܻ�ȡ��ʱ�������ֵ����Сֵ�Ĺ�ϵ�����Ծͱ������
                     && !DA.GetData(4, ref s)) //�޷���ȡ����
                return;
            #endregion

            const double chordR = 1.2740056;//����ת�ҳ�ϵ����1/3����arcsin(12/13)*13/12
            var rmax = (max / chordR);//��С����ת��С�ҳ�
            var rmin = (min / chordR);//��󻡳�ת����ҳ�
            var l = b.GetLength();//�߿򳤶�
           
            double[] ra;
            int c;
            if (bmax)
            {
                c = (int)Math.Floor(l *2/ (rmax + rmin));//����r��ֵ�� ��ӽ���С������
                var r1 = l / c;//ʵ�ʵ�ƽ��ֵ
                var c1 = c / 2;
                var rx = rmax - rmin;//��ֵ
                ra = new double[c] ; //��ͷ
                var c2 = c1;//����һ��ļ��
                Random ran = new Random(s);
                if (c % 2 == 1) 
                {
                    ra[c1] = r1; //���м��ֵ
                    c2++;//����ʱ�����1
                }//������������м丳ֵ���������ڶ��ε���ʼλ��

                for (int i = 0; i < c1; i++)
                {
                    double d0=(ran.NextDouble()-0.5) *rx;
                    ra[i] = r1 + d0;
                    ra[i + c2] = r1 - d0;
                }//�����������
            }
            else
            {
                c = (int)Math.Floor(l / rmin);
                ra = Enumerable.Repeat(l / c, c).ToArray();
            }
            for (int i = 1; i < c - 1; i++)
                ra[i] += ra[i - 1];//�������ȱ�ɵ��ӳ���
            ra = new double[] { 0 }.Concat(ra).ToArray();//��ͷ
            ra[c] = 0; //��β

            b.TryGetPlane(out Plane plane);//��ȡ����ƽ��
            var za = plane.ZAxis;//��ȡƽ��Z��
            const double angle = -Math.PI / 2;//��ת�Ƕ�
            var arca = new Polyline(ra.Select(t => b.PointAtLength(t)))
                //�����Ȼ�ȡ�㣬��ת�����
                .GetSegments()//��ȡȫ���߶�
                .Select(t =>
                {
                    var v = t.Direction / 3;//��ȡ1/3���ȵ�����
                    v.Rotate(r ? angle : -angle, za);//��ת����,r���򷴷���
                    return new Arc(t.From, (t.To + t.From) / 2 + v, t.To).ToNurbsCurve();
                }).ToArray();

            DA.SetData(0, Curve.JoinCurves(arca)[0]);
            DA.SetData(1, arca.Select(t => t.GetLength()).ToInterval());
        }
    }

}