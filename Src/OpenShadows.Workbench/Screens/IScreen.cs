using System;
using System.Collections.Generic;
using System.Text;
using Veldrid.Sdl2;

namespace OpenShadows.Workbench.Screens
{
	public interface IScreen
	{
		void Update(float dt);

		void Render(Sdl2Window window);

		string Title { get; }
	}
}
