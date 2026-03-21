using System.Security.Principal;

namespace HEMA
{
	public class BracketInfo
	{
		public string BracketId { get; set; }	

		public BracketType BracketType { get; set; }
		
		public BracketOrientation? BracketOrientation { get; set; }
	}

	public enum BracketType  
	{
		Winner,
		Looser,
		Final
	}

	public enum BracketOrientation
	{
		Left,
		Right,
	}
}