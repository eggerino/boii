#[cfg(feature = "headless")]
mod headless;

#[cfg(feature = "raylib")]
mod raylib;

#[cfg(feature = "headless")]
pub use headless::Window;

#[cfg(feature = "raylib")]
pub use raylib::Window;
