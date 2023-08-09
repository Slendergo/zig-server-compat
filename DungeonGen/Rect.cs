using System;

namespace DungeonGenerator
{

	// Token: 0x02000038 RID: 56

	public struct Rect

	{

		// Token: 0x060001B9 RID: 441 RVA: 0x0000666D File Offset: 0x0000486D

		public Rect(int x, int y, int maxX, int maxY)

		{

			this.X = x;

			this.Y = y;

			this.MaxX = ((maxX < x) ? x : maxX);

			this.MaxY = ((maxY < y) ? y : maxY);

		}



		// Token: 0x060001BA RID: 442 RVA: 0x0000669B File Offset: 0x0000489B

		public Rect(double x, double y, double maxX, double maxY)
		{
			this = new Rect((int)Math.Round(x), (int)Math.Round(x), (int)Math.Round(maxX), (int)Math.Round(maxY));
		}



		// Token: 0x060001BB RID: 443 RVA: 0x000066C0 File Offset: 0x000048C0

		public bool Contains(Point pt)

		{

			return this.Contains(pt.X, pt.Y);

		}



		// Token: 0x060001BC RID: 444 RVA: 0x000066D6 File Offset: 0x000048D6

		public bool Contains(double x, double y)

		{

			return x >= (double)this.X && x < (double)this.MaxX && y >= (double)this.Y && y < (double)this.MaxY;

		}



		// Token: 0x060001BD RID: 445 RVA: 0x00006702 File Offset: 0x00004902

		public bool Contains(int x, int y)

		{

			return x >= this.X && x < this.MaxX && y >= this.Y && y < this.MaxY;

		}



		// Token: 0x17000074 RID: 116

		// (get) Token: 0x060001BE RID: 446 RVA: 0x0000672A File Offset: 0x0000492A

		public bool IsEmpty

		{

			get

			{

				return this.X == this.MaxX || this.Y == this.MaxY;

			}

		}



		// Token: 0x060001BF RID: 447 RVA: 0x0000674C File Offset: 0x0000494C

		public Rect Intersection(Rect rect)

		{

			return new Rect(Math.Max(this.X, rect.X), Math.Max(this.Y, rect.Y), Math.Min(this.MaxX, rect.MaxX), Math.Min(this.MaxY, rect.MaxY));

		}



		// Token: 0x060001C0 RID: 448 RVA: 0x000067A8 File Offset: 0x000049A8

		public override string ToString()

		{

			return string.Format("({0}, {1}, {2}, {3})", new object[] { this.X, this.MaxX, this.Y, this.MaxY });

		}



		// Token: 0x04000176 RID: 374

		public static readonly Rect Empty = default(Rect);



		// Token: 0x04000177 RID: 375

		public readonly int MaxX;



		// Token: 0x04000178 RID: 376

		public readonly int MaxY;



		// Token: 0x04000179 RID: 377

		public readonly int X;



		// Token: 0x0400017A RID: 378

		public readonly int Y;

	}

}
