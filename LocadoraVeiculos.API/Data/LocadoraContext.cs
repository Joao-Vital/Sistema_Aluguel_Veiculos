using LocadoraVeiculos.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LocadoraVeiculos.API.Data
{
    public class LocadoraContext : DbContext
    {
        public LocadoraContext(DbContextOptions<LocadoraContext> options) : base(options)
        {
        }

        public DbSet<Fabricante> Fabricantes { get; set; } = null!;
        public DbSet<CategoriaVeiculo> CategoriasVeiculo { get; set; } = null!;
        public DbSet<Veiculo> Veiculos { get; set; } = null!;
        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<Aluguel> Alugueis { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Fabricante ----------
            modelBuilder.Entity<Fabricante>()
                .HasIndex(f => f.Nome)
                .IsUnique();

            // ---------- CategoriaVeiculo ----------
            modelBuilder.Entity<CategoriaVeiculo>()
                .HasIndex(c => c.Nome)
                .IsUnique();

            // ---------- Cliente ----------
            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.CPF)
                .IsUnique();

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Email)
                .IsUnique();

            // ---------- Veiculo ----------
            modelBuilder.Entity<Veiculo>()
                .HasIndex(v => v.Placa)
                .IsUnique();

            // Veiculo -> Fabricante (N:1). Restrict para não apagar fabricante em cascata
            // e evitar múltiplos caminhos de cascade delete no SQL Server.
            modelBuilder.Entity<Veiculo>()
                .HasOne(v => v.Fabricante)
                .WithMany(f => f.Veiculos)
                .HasForeignKey(v => v.FabricanteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Veiculo -> CategoriaVeiculo (N:1)
            modelBuilder.Entity<Veiculo>()
                .HasOne(v => v.CategoriaVeiculo)
                .WithMany(c => c.Veiculos)
                .HasForeignKey(v => v.CategoriaVeiculoId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- Aluguel ----------
            // Aluguel -> Cliente (N:1)
            modelBuilder.Entity<Aluguel>()
                .HasOne(a => a.Cliente)
                .WithMany(c => c.Alugueis)
                .HasForeignKey(a => a.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Aluguel -> Veiculo (N:1)
            modelBuilder.Entity<Aluguel>()
                .HasOne(a => a.Veiculo)
                .WithMany(v => v.Alugueis)
                .HasForeignKey(a => a.VeiculoId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- Dados iniciais (seed) opcionais para facilitar os testes no Swagger ----------
            modelBuilder.Entity<CategoriaVeiculo>().HasData(
                new CategoriaVeiculo { CategoriaVeiculoId = 1, Nome = "Popular", Descricao = "Veículos econômicos", ValorDiariaBase = 90.00m },
                new CategoriaVeiculo { CategoriaVeiculoId = 2, Nome = "SUV", Descricao = "Utilitários esportivos", ValorDiariaBase = 180.00m },
                new CategoriaVeiculo { CategoriaVeiculoId = 3, Nome = "Luxo", Descricao = "Veículos de alto padrão", ValorDiariaBase = 350.00m }
            );

            modelBuilder.Entity<Fabricante>().HasData(
                new Fabricante { FabricanteId = 1, Nome = "Chevrolet", PaisOrigem = "Estados Unidos" },
                new Fabricante { FabricanteId = 2, Nome = "Fiat", PaisOrigem = "Itália" },
                new Fabricante { FabricanteId = 3, Nome = "Toyota", PaisOrigem = "Japão" }
            );
        }
    }
}
