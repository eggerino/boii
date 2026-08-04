use crate::io;
use raylib::prelude::*;

const BACKGROUND_COLOR: Color = Color::BLACK;

// Gameboy has a 160x144 resolution
const BUF_WIDTH: i32 = 160;
const BUF_HEIGHT: i32 = 144;

type PixelBuffer<T> = [T; (BUF_WIDTH * BUF_HEIGHT) as usize];

fn idx(x: i32, y: i32) -> usize {
    y.wrapping_mul(BUF_WIDTH).wrapping_add(x) as usize
}

impl io::Color {
    fn into_raylib(self) -> Color {
        match self {
            Self::White => Color::WHITE,
            Self::LightGray => Color::LIGHTGRAY,
            Self::DarkGray => Color::DARKGRAY,
            Self::Black => Color::BLACK,
        }
    }
}

pub struct Window {
    handle: RaylibHandle,
    thread: RaylibThread,
    buf: PixelBuffer<io::Color>,
}

impl Window {
    pub fn new(title: &str) -> Self {
        let (mut handle, thread) = raylib::init()
            .log_level(TraceLogLevel::LOG_ERROR)
            .size(800, 600)
            .resizable()
            .title(title)
            .build();

        // Ensure the size is at least the gameboy resolution
        // since no fractional scaling is implemented.
        // Else no scene would get rendered (scale = 0).
        handle.set_window_min_size(BUF_WIDTH, BUF_HEIGHT);

        Self {
            handle,
            thread,
            buf: [io::Color::Black; (BUF_WIDTH * BUF_HEIGHT) as usize],
        }
    }

    pub fn should_close(&self) -> bool {
        self.handle.window_should_close()
    }
}

impl io::Draw for Window {
    fn set_pixel(&mut self, x: i32, y: i32, color: io::Color) {
        if let Some(c) = self.buf.get_mut(idx(x, y)) {
            *c = color;
        }
    }

    fn update(&mut self) {
        let width = self.handle.get_screen_width();
        let height = self.handle.get_screen_height();
        let scale = (width / BUF_WIDTH).min(height / BUF_HEIGHT);

        let mut d = self.handle.begin_drawing(&self.thread);
        d.clear_background(BACKGROUND_COLOR);

        for x in 0..BUF_WIDTH {
            let px = (width / 2).wrapping_add(x.wrapping_sub(BUF_WIDTH / 2).wrapping_mul(scale));

            for y in 0..BUF_HEIGHT {
                let py =
                    (height / 2).wrapping_add(y.wrapping_sub(BUF_HEIGHT / 2).wrapping_mul(scale));

                let color = self
                    .buf
                    .get(idx(x, y))
                    .map(|x| x.into_raylib())
                    .unwrap_or(BACKGROUND_COLOR);

                d.draw_rectangle(px, py, scale, scale, color);
            }
        }

        d.draw_fps(0, 0);
    }
}

impl io::Gamepad for Window {
    fn left(&self) -> bool {
        self.handle.is_key_down(KeyboardKey::KEY_A)
    }

    fn right(&self) -> bool {
        self.handle.is_key_down(KeyboardKey::KEY_D)
    }

    fn up(&self) -> bool {
        self.handle.is_key_down(KeyboardKey::KEY_W)
    }

    fn down(&self) -> bool {
        self.handle.is_key_down(KeyboardKey::KEY_S)
    }

    fn a(&self) -> bool {
        self.handle.is_key_down(KeyboardKey::KEY_ENTER)
    }

    fn b(&self) -> bool {
        self.handle.is_key_down(KeyboardKey::KEY_SPACE)
    }

    fn start(&self) -> bool {
        self.handle.is_key_down(KeyboardKey::KEY_BACKSPACE)
    }

    fn select(&self) -> bool {
        self.handle.is_key_down(KeyboardKey::KEY_DELETE)
    }
}
