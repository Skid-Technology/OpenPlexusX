using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class ParticleField : IBackdrop
{
	private struct Node
	{
		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public float Phase;

		public float Size;

		public bool Big;

		public Color Color;
	}

	private readonly struct Segment(float x1, float y1, float x2, float y2, int a)
	{
		public readonly float X1 = x1;

		public readonly float Y1 = y1;

		public readonly float X2 = x2;

		public readonly float Y2 = y2;

		public readonly int Alpha = a;
	}

	private readonly Node[] _nodes;

	private readonly List<Segment> _segments = new List<Segment>();

	private readonly Random _rng = new Random();

	private int _w;

	private int _h;

	private double _t;

	private float Threshold => Math.Clamp((float)Math.Min(_w, _h) * 0.26f, 120f, 220f);

	public bool Moves => true;

	public ParticleField(int count)
	{
		_nodes = new Node[count];
	}

	public void Resize(int w, int h)
	{
		bool num = _w == 0 && _h == 0;
		_w = Math.Max(1, w);
		_h = Math.Max(1, h);
		if (num)
		{
			for (int i = 0; i < _nodes.Length; i++)
			{
				Spawn(ref _nodes[i]);
			}
			BuildSegments();
		}
	}

	private void Spawn(ref Node n)
	{
		n.X = (float)(_rng.NextDouble() * (double)_w);
		n.Y = (float)(_rng.NextDouble() * (double)_h);
		double num = _rng.NextDouble() * Math.PI * 2.0;
		float num2 = 8f + (float)_rng.NextDouble() * 15f;
		n.Vx = (float)Math.Cos(num) * num2;
		n.Vy = (float)Math.Sin(num) * num2;
		n.Phase = (float)(_rng.NextDouble() * Math.PI * 2.0);
		n.Big = _rng.NextDouble() < 0.4;
		n.Size = (n.Big ? (3f + (float)_rng.NextDouble() * 2f) : (1.8f + (float)_rng.NextDouble() * 1.2f));
		n.Color = ((_rng.NextDouble() < 0.5) ? Theme.PlexusNodeA : Theme.PlexusNodeB);
	}

	public void Update(double dtSeconds)
	{
		_t += dtSeconds;
		float num = (float)dtSeconds;
		for (int i = 0; i < _nodes.Length; i++)
		{
			ref Node reference = ref _nodes[i];
			reference.X += reference.Vx * num;
			reference.Y += reference.Vy * num;
			if (reference.X < 0f)
			{
				reference.X = 0f;
				reference.Vx = 0f - reference.Vx;
			}
			else if (reference.X > (float)_w)
			{
				reference.X = _w;
				reference.Vx = 0f - reference.Vx;
			}
			if (reference.Y < 0f)
			{
				reference.Y = 0f;
				reference.Vy = 0f - reference.Vy;
			}
			else if (reference.Y > (float)_h)
			{
				reference.Y = _h;
				reference.Vy = 0f - reference.Vy;
			}
		}
		BuildSegments();
	}

	private void BuildSegments()
	{
		_segments.Clear();
		float threshold = Threshold;
		float num = threshold * threshold;
		for (int i = 0; i < _nodes.Length; i++)
		{
			for (int j = i + 1; j < _nodes.Length; j++)
			{
				float num2 = _nodes[i].X - _nodes[j].X;
				float num3 = _nodes[i].Y - _nodes[j].Y;
				float num4 = num2 * num2 + num3 * num3;
				if (!(num4 >= num))
				{
					float num5 = (float)Math.Sqrt(num4);
					int num6 = (int)((1f - num5 / threshold) * 95f);
					if (num6 > 3)
					{
						_segments.Add(new Segment(_nodes[i].X, _nodes[i].Y, _nodes[j].X, _nodes[j].Y, num6));
					}
				}
			}
		}
	}

	public void Paint(Graphics g, int offsetX, int offsetY)
	{
		GraphicsState gstate = g.Save();
		g.TranslateTransform(-offsetX, -offsetY);
		PaintCore(g);
		g.Restore(gstate);
	}

	private void PaintCore(Graphics g)
	{
		if (_w <= 1 || _h <= 1)
		{
			return;
		}
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Color plexusLine = Theme.PlexusLine;
		using (Pen pen = new Pen(plexusLine, 1f))
		{
			foreach (Segment segment in _segments)
			{
				pen.Color = Color.FromArgb(segment.Alpha, plexusLine);
				g.DrawLine(pen, segment.X1, segment.Y1, segment.X2, segment.Y2);
			}
		}
		using SolidBrush solidBrush = new SolidBrush(Color.White);
		Node[] nodes = _nodes;
		for (int i = 0; i < nodes.Length; i++)
		{
			Node node = nodes[i];
			int num = (int)((0.7f + 0.3f * (float)Math.Sin(_t * 2.0 + (double)node.Phase)) * 230f);
			if (node.Big)
			{
				solidBrush.Color = Color.FromArgb(Math.Min(60, num / 3), node.Color);
				float num2 = node.Size * 4.5f;
				g.FillEllipse(solidBrush, node.X - num2 / 2f, node.Y - num2 / 2f, num2, num2);
			}
			solidBrush.Color = Color.FromArgb(Math.Min(240, num), node.Color);
			g.FillEllipse(solidBrush, node.X - node.Size / 2f, node.Y - node.Size / 2f, node.Size, node.Size);
		}
	}
}
