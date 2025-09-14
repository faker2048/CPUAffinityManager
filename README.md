# CPU Affinity Manager

<img width="788" height="444" alt="image" src="https://github.com/user-attachments/assets/39bccc9f-dc7f-4839-95aa-fdafccb66f57" />


A Windows utility designed specifically for controlling game and application scheduling on AMD multi-CCD processors such as 7950X3D and 9950X3D. This tool gives you direct control over which CCD (Core Compute Die) your games run on, helping optimize performance and reduce latency.


## Features

- Set specific CPU cores for individual processes
- Group cores into CCD configurations
- Apply affinity rules manually or automatically when processes start
- Visual interface to monitor and manage process affinities
- Persistent configuration using TOML format

## Use Cases

- Optimize game performance by binding games to specific CCDs
- Improve latency-sensitive application performance
- Reduce cross-CCD communication overhead
- Balance system workload across CCDs

## Technical Details

- Built with .NET 9.0 for Windows
- WPF UI with MVVM architecture using CommunityToolkit.Mvvm
- Stores configuration in user's AppData folder
- Supports up to 64 cores (0-63)

## Getting Started

1. Download and run the application
2. Create CCD groups with specific CPU cores
3. Add processes to monitor and assign them to CCD groups
4. Apply the rules manually or enable auto-apply

## Configuration

The application stores its configuration in `%AppData%\CPUAffinityManager\config.toml` in TOML format:

```toml
[ccds.ccd0]
cores = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15]

[ccds.ccd1]
cores = [16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31]

[process_bind]
game1 = "ccd0"
game2 = "ccd1"
```

## License

MIT.

## Contributing

Contributions are welcome! Feel free to submit a pull request or open an issue. 
