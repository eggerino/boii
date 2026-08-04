#[derive(Clone, Copy)]
pub enum Color {
    White,
    LightGray,
    DarkGray,
    Black,
}

pub trait Draw {
    fn set_pixel(&mut self, x: i32, y: i32, color: Color);
    fn update(&mut self);
}

pub trait Gamepad {
    fn left(&self) -> bool;
    fn right(&self) -> bool;
    fn up(&self) -> bool;
    fn down(&self) -> bool;
    fn a(&self) -> bool;
    fn b(&self) -> bool;
    fn start(&self) -> bool;
    fn select(&self) -> bool;
}
