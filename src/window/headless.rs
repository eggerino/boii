use crate::io;

pub struct Window;

impl Window {
    pub fn new(_title: &str) -> Self {
        Self
    }

    pub fn should_close(&self) -> bool {
        false
    }
}

impl io::Draw for Window {
    fn set_pixel(&mut self, _x: i32, _y: i32, _color: io::Color) {}

    fn update(&mut self) {}
}

impl io::Gamepad for Window {
    fn left(&self) -> bool {
        false
    }

    fn right(&self) -> bool {
        false
    }

    fn up(&self) -> bool {
        false
    }

    fn down(&self) -> bool {
        false
    }

    fn a(&self) -> bool {
        false
    }

    fn b(&self) -> bool {
        false
    }

    fn start(&self) -> bool {
        false
    }

    fn select(&self) -> bool {
        false
    }
}
