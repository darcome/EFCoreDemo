using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

RepositoryContext rc = new ();
UserClaim c1 = new UserClaim ()
{
	Id = 1,
	Name = "Administrator"
};

UserClaim c2 = new UserClaim ()
{
	Id = 2,
	Name = "Moderator"
};

rc.UserClaims.Add (c1);
rc.UserClaims.Add (c2);

User u1 = new User ()
{
	Id = 1,
	Email = "aaa@aaa.com",
	Username = "aaa",
	Claims = new List<UserClaim> { c1 }
};

User u2 = new User ()
{
	Id = 2,
	Email = "bbb@bbb.com",
	Username = "bbb",
	Claims = new List<UserClaim> { c1 }
};

rc.Users.Add (u1);
rc.Users.Add (u2);

await rc.SaveChangesAsync ();

List<User> users = await rc.Users.ToListAsync ();

Console.WriteLine ("All users:");
PrintUsers (users);
Console.WriteLine ("--------------------------");

List<long> ids = new () {1, 2};
IQueryable<User> query = rc.Users.Where (u => u.Claims.All (c => ids.Contains (c.Id)));
string sql = query.ToQueryString ();

Console.WriteLine ($"Searching for users who are both {c1.Name}({c1.Id}) and {c2.Name}({c2.Id}) with the following query:");
Console.WriteLine ($"{sql}");
Console.WriteLine ("--------------------------");

users = await query.ToListAsync ();
Console.WriteLine ("Results:");
PrintUsers (users);
Console.WriteLine ("--------------------------");

rc.Users.Remove (u1);
rc.Users.Remove (u2);

rc.UserClaims.Remove (c1);
rc.UserClaims.Remove (c2);

await rc.SaveChangesAsync ();

void PrintUsers (List<User> users)
{
	foreach (User u in users)
	{
		Console.WriteLine ($"{u.Id} {u.Username} {string.Join (", ", u.Claims.Select (x => x.Name).ToList ())}");
	}
}

public class BaseEntity
{
	public	long		Id				{get;set;} = 0;
	public	DateTime	Created			{get;set;} = DateTime.MinValue.ToUniversalTime ();
	public	DateTime	LastModified	{get;set;} = DateTime.MinValue.ToUniversalTime ();
	public	bool		IsArchived		{get;set;} = false;
}

public sealed class UserClaim : BaseEntity
{
#region Variables
#endregion
// ------------------------------------------------------------------------------------ //
#region Properties
	public			string				Name				{get;set;} = string.Empty;
#endregion
// ------------------------------------------------------------------------------------ //
#region Constructors
	public UserClaim ()
	{

	}
// ------------------------------------------------------------------------------------ //
#endregion
// ------------------------------------------------------------------------------------ //
}

public class User : BaseEntity
{
#region Variables
#endregion
// ------------------------------------------------------------------------------------ //
#region Properties
	public			string					Email								{get;set;}	= string.Empty;
	public			string					Username							{get;set;}	= string.Empty;
	public			string					PasswordHash						{get;set;}	= string.Empty;
	public	virtual	List<UserClaim>			Claims								{get;set;}	= new ();
#endregion
// ------------------------------------------------------------------------------------ //
#region Constructors
	public User ()
	{

	}
// ------------------------------------------------------------------------------------ //
#endregion
// ------------------------------------------------------------------------------------ //
}

public sealed class RepositoryContext : DbContext
{
#region Variables
#endregion
// ------------------------------------------------------------------------------------ //
#region Properties
	public DbSet<User>?			Users			{get;set;}
	public DbSet<UserClaim>?	UserClaims		{get;set;}
#endregion
// ------------------------------------------------------------------------------------ //
#region Constructors
#endregion
// ------------------------------------------------------------------------------------ //
#region Methods
	protected override void OnConfiguring(DbContextOptionsBuilder builder)
    { 
        builder.UseMySql ("Server=192.168.230.3;Port=3306;Database=agilista.it;User=root;Password=root;DateTimeKind=Utc", 
						ServerVersion.AutoDetect ("Server=192.168.230.3;Port=3306;Database=agilista.it;User=root;Password=root;DateTimeKind=Utc"), 
						b => b.UseMicrosoftJson ().EnableRetryOnFailure ());
    }
	protected	override	void		OnModelCreating			(ModelBuilder modelBuilder)
	{
		bool seedData = false;

		modelBuilder.ApplyConfiguration (new UserConfiguration			(seedData));
		modelBuilder.ApplyConfiguration (new UserClaimConfiguration		(seedData));
	}
// ------------------------------------------------------------------------------------ //
#endregion
// ------------------------------------------------------------------------------------ //
}

public class BaseConfiguration<T> where T : BaseEntity
{
#region Variables
	protected	bool	seedData		= false;
#endregion
// ------------------------------------------------------------------------------------ //
#region Constructors
	public BaseConfiguration (bool seedData)
	{
		this.seedData = seedData;
	}
// ------------------------------------------------------------------------------------ //
#endregion
// ------------------------------------------------------------------------------------ //
#region Methods
	protected		void				AddIndexWithFilter						(EntityTypeBuilder<T> builder, Expression<Func<T, object?>> expression, 
																				bool isUnique)
	{
		IndexBuilder<T> index =  builder.HasIndex (expression);
		
		if (isUnique)
			index.IsUnique ();
		
		index.HasFilter ($"{builder.Metadata.GetTableName ()}.\"{nameof (BaseEntity.IsArchived)}\" = false");
	}
// ------------------------------------------------------------------------------------ //
	protected		void				AddIndexWithFilter						(EntityTypeBuilder<T> builder, string[] fields, bool isUnique)
	{
		IndexBuilder<T> index =  builder.HasIndex (fields);
		
		if (isUnique)
			index.IsUnique ();
		
		index.HasFilter ($"{builder.Metadata.GetTableName ()}.\"{nameof (BaseEntity.IsArchived)}\" = false");
	}
// ------------------------------------------------------------------------------------ //
	protected		void				AddIndexWithFilter						(EntityTypeBuilder<T> builder, Expression<Func<T, object?>> expression, 
																				bool isUnique, string filter)
	{
		IndexBuilder<T> index =  builder.HasIndex (expression);
		
		if (isUnique)
			index.IsUnique ();
		
		index.HasFilter (filter);
	}
// ------------------------------------------------------------------------------------ //
	protected		void				ConfigureCommon							(EntityTypeBuilder<T> builder, int order)
	{
		builder.Property (x => x.Id).HasColumnOrder (0);
		builder.Property (x => x.IsArchived)	.IsRequired (true).HasColumnOrder (order++).HasDefaultValue (false);
		builder.Property (x => x.Created)		.IsRequired (true).HasColumnOrder (order++).HasDefaultValueSql ("now()");
		builder.Property (x => x.LastModified)	.IsRequired (true).HasColumnOrder (order++).HasDefaultValueSql ("now()");

		builder.HasKey (x => x.Id);

		builder.HasIndex (x => x.IsArchived);
	}
// ------------------------------------------------------------------------------------ //
#endregion
// ------------------------------------------------------------------------------------ //
}

public sealed class UserClaimConfiguration : BaseConfiguration<UserClaim>, IEntityTypeConfiguration<UserClaim>
{
#region Constructors
	public UserClaimConfiguration (bool seedData) : base (seedData)
	{
	}
// ------------------------------------------------------------------------------------ //
#endregion
// ------------------------------------------------------------------------------------ //
#region Methods
	public		void			Configure						(EntityTypeBuilder<UserClaim> builder)
	{
		builder.ToTable ("user_claims");

		int order = 1;

		builder.Property (x => x.Name).IsRequired (true).HasColumnOrder (order++).HasMaxLength (100).HasDefaultValue (string.Empty);

		ConfigureCommon (builder, order);

		AddIndexWithFilter (builder, x => x.Name, true);
	}
// ------------------------------------------------------------------------------------ //
#endregion
// ------------------------------------------------------------------------------------ //
}

public sealed class UserConfiguration : BaseConfiguration<User>, IEntityTypeConfiguration<User>
{
#region Constructors
	public UserConfiguration (bool seedData) : base (seedData)
	{
	}
// ------------------------------------------------------------------------------------ //
#endregion
// ------------------------------------------------------------------------------------ //
#region Methods
	public		void			Configure						(EntityTypeBuilder<User> builder)
	{
		builder.ToTable ("users");

		int order = 1;

		builder.Property (x => x.Email).IsRequired							(true).HasColumnOrder (order++).HasMaxLength (100);
		builder.Property (x => x.Username).IsRequired						(true).HasColumnOrder (order++).HasMaxLength (100).HasDefaultValue (string.Empty);
		builder.Property (x => x.PasswordHash).IsRequired					(true).HasColumnOrder (order++).HasMaxLength (100).HasDefaultValue (string.Empty);

		ConfigureCommon (builder, order);

		AddIndexWithFilter (builder, x => x.Email,			true);
		AddIndexWithFilter (builder, x => x.Username,		true);
		
		builder.HasMany		(x => x.Claims).WithMany ().UsingEntity (join => join.ToTable ("users_userclaims"));
	}
// ------------------------------------------------------------------------------------ //
#endregion
// ------------------------------------------------------------------------------------ //
}