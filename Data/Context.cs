using Microsoft.EntityFrameworkCore;
using myfreelas.Models;

namespace myfreelas.Data;

public class Context : DbContext
{
    public Context(DbContextOptions<Context> options) : base (options) {}

    public DbSet<User> Users { get; set; }
    public DbSet<Customer> Customers { get; set; }
}
